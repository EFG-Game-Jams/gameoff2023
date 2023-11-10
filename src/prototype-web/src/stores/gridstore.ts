import { defineStore } from "pinia";
import {
  IEdge,
  IEdgeDirectionProperties,
  IGridNode,
  IPacketsInTransit as IEdgePayload,
  IPlayer,
IGridNodePlayerProperties,
} from "@/types";
import { createPlayer } from "@/factories";

interface IEdgePayloadDelivery extends IEdgePayload {
  destinationNodeId: number;
}

type StoreState = {
  nodes: IGridNode[];
  edges: IEdge[];
  players: IPlayer[];
};

function getEmptyState(): StoreState {
  return {
    nodes: [],
    edges: [],
    players: [
      createPlayer(1, "overflow"),
      createPlayer(2, "overflow"),
      createPlayer(3, "overflow"),
      createPlayer(4, "overflow"),
    ],
  };
}

export const useGridStore = defineStore("grid-store", {
  state: getEmptyState,
  actions: {
    clear() {
      this.$state = getEmptyState();
    },
    resetPlayerProgression() {
      this.nodes.forEach((n) => {
        n.capacityUsed = 0;
        n.currentlyOwnedByPlayerId = n.spawnOwnedByPlayerId;
      });
      this.edges.forEach((e) => {
        e.originToDestinationProperties.payloads = [];
        e.destinationToOriginProperties.payloads = [];
      });
    },
    tick() {
      // Produce
      this.nodes.forEach((n) => {
        n.capacityUsed += n.packetProductionPerTick;
      });

      // Tick packets in transit
      this.edges.forEach((e) => {
        e.originToDestinationProperties.payloads.forEach(
          (p) => ++p.elapsedTicks
        );
        e.destinationToOriginProperties.payloads.forEach(
          (p) => ++p.elapsedTicks
        );
      });

      // Handle incoming packets on nodes
      let deliveriesToProcess: IEdgePayloadDelivery[] = [];
      this.edges.forEach((e) => {
        deliveriesToProcess = [
          ...deliveriesToProcess,
          ...extractPayloadsToDeliver(
            e.destinationNodeId,
            e.originToDestinationProperties
          ),
          ...extractPayloadsToDeliver(
            e.originNodeId,
            e.destinationToOriginProperties
          ),
        ];
      });
      this.nodes.forEach((n) => {
        deliveriesToProcess
          .filter((d) => d.destinationNodeId === n.id)
          // Group by player ID
          .reduce<{ packetCount: number; playerId: number }[]>(
            (accum, currentValue) => {
              let playerDelivery = accum.find(
                (a) => a.playerId === currentValue.playerId
              );
              if (playerDelivery == null) {
                playerDelivery = {
                  playerId: currentValue.playerId,
                  packetCount: currentValue.packetCount,
                };
                accum.push(playerDelivery);
              } else {
                playerDelivery.packetCount += currentValue.packetCount;
              }
              return accum;
            },
            []
          )
          .forEach((p) => {
            if (n.currentlyOwnedByPlayerId == null) {
              n.capacityUsed = p.packetCount;
              n.currentlyOwnedByPlayerId = p.playerId;
            } else if (p.playerId === n.currentlyOwnedByPlayerId) {
              n.capacityUsed += p.packetCount;
            } else {
              n.capacityUsed = Math.round(n.capacityUsed - p.packetCount);
              if (n.capacityUsed < 0) {
                n.currentlyOwnedByPlayerId = p.playerId;
                n.capacityUsed = -1 * n.capacityUsed;
              } else if (n.capacityUsed === 0) {
                n.currentlyOwnedByPlayerId = null;
              }
            }
          });
      });

      // Handle outflow through balancing/fluid algorithm
      // TODO shuffle the edge array to avoid bias
      this.nodes.forEach((n) => {
        calculatePacketsToDispatch(
          n,
          this.edges,
          this.players);
      });

      // Trim excess capacity
      this.nodes.forEach((n) => {
        // Consider disabling this line if overfilling is no longer an issue
        n.capacityUsed = Math.min(n.capacityUsed, n.capacityLimit);
        n.capacityUsed = Math.max(n.capacityUsed, 0);
      });
    },
  },
  getters: {},
});

function calculatePacketsToDispatch(
  node: IGridNode,
  edges: IEdge[],
  players: IPlayer[]): void {
  if (!properties.enabled) {
    return;
  }

  const destinationNode = nodes.find((n) => n.id === destinationNodeId);
  if (destinationNode == null) {
    return;
  }
  const destinationNodePlayerProperties =
    destinationNode.playerProperties.find(
      (fp) => fp.playerId === player.id
    );
  if (destinationNodePlayerProperties == null || destinationNodePlayerProperties.disableInflow) {
    return;
  }

  const originNode = nodes.find((n) => n.id === originNodeId);
  if (originNode == null) {
    return;
  }
  const originNodePlayerProperties = originNode.playerProperties.find(
    (fp) => fp.playerId === player.id
  );
  if (originNodePlayerProperties == null) {
    return;
  }

  const destinationFillRatio = destinationNode.currentlyOwnedByPlayerId === player.id ?
    (destinationNode.capacityUsed - destinationNodePlayerProperties.intendedFillRatio)
}

// Returns the balance expressed in the range -1 to 1
// Ex 1: Node is half full, intended fill ratio is at half, result is 0 (perfectly balanced)
// Ex 2: Node is half full, intended fill ratio is at three quarters, result is -0.25 (negative pressure)
// Ex 3: Node is half full, intended fill ratio is at one quarters, result is 0.25 (positive pressure)
// Ex 4: Node is half full, intended fill ratio is at zero, result is -0.5 (negative pressure)
// Ex 4: Node is at zero, intended fill ratio is at half, result is 0.5 (positive pressure)
function getNodeBalance(node: IGridNode, incomingEdges: IEdge[], playerProperties: IGridNodePlayerProperties): number {
  if (node.currentlyOwnedByPlayerId != playerProperties.playerId) {
    // The node is owned by the enemy but it is wanted. Commit the resources as dictated
    // by the intended fill ratio directly
    return playerProperties.intendedFillRatio;
  }

  // The node is owned by the player but it is intended to be empty
  if (playerProperties.intendedFillRatio <= 0) {
    return 1;
  }

  // Some fraction between 0 and 1 (inclusive)
  const currentBalance = node.capacityUsed / node.capacityLimit;
  // Some number between -1 and 1, 'negative' pressure to 'positive pressure), i.e.
  // a node at negative pressure 'sucks' and a node at positive pressure 'pushes'
  return currentBalance - playerProperties.intendedFillRatio;
}

function extractPayloadsToDeliver(
  destinationNodeId: number,
  properties: IEdgeDirectionProperties
): IEdgePayloadDelivery[] {
  const transferred: IEdgePayloadDelivery[] = [];
  for (let i = properties.payloads.length - 1; i >= 0; --i) {
    if (
      properties.payloads[i].elapsedTicks >= properties.transferLatencyInTicks
    ) {
      transferred.push({ destinationNodeId, ...properties.payloads[i] });
      properties.payloads.splice(i, 1);
    }
  }

  return transferred;
}
