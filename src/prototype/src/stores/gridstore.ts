import { defineStore } from "pinia";
import {
  IEdge,
  IEdgeDirectionProperties,
  IGridNode,
  IPacketsInTransit as IEdgePayload,
  IPlayer,
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

      // Dispatch
      this.nodes.forEach((n) => {
        if (n.capacityUsed < 1 || n.currentlyOwnedByPlayerId == null) {
          return;
        }

        // TODO handle flows (manual flow or else default flow)
        // TODO use edge bandwidth instead of infinity
        // Temporary endless circulation algorithm
        const dispatchEdges = this.edges.filter(
          (e) =>
            (e.destinationNodeId === n.id &&
              e.destinationToOriginProperties.enabled) ||
            (e.originNodeId === n.id && e.originToDestinationProperties.enabled)
        );
        const packetsPerEdge = n.capacityUsed / dispatchEdges.length;
        if (packetsPerEdge < 1) {
          for (let i = 0; i < dispatchEdges.length; ++i) {
            dispatchOnEdge(n, dispatchEdges[i], 1);
          }
        } else {
          dispatchEdges.forEach((e) => {
            dispatchOnEdge(n, e, Math.ceil(packetsPerEdge));
          });
        }
        n.capacityUsed = 0;
      });

      // Receive
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

      // Trim excess capacity
      this.nodes.forEach((n) => {
        n.capacityUsed = Math.min(n.capacityUsed, n.capacityLimit);
        n.capacityUsed = Math.max(n.capacityUsed, 0);
      });
    },
  },
  getters: {},
});

function dispatchOnEdge(
  origin: IGridNode,
  edge: IEdge,
  packetCount: number
): void {
  if (origin.currentlyOwnedByPlayerId == null) {
    throw Error(
      "Cannot dispatch packets from neutral node (missing Player ID)"
    );
  }

  if (edge.originNodeId == origin.id) {
    edge.originToDestinationProperties.payloads.push({
      packetCount,
      playerId: origin.currentlyOwnedByPlayerId,
      elapsedTicks: 0,
    });
  } else {
    edge.destinationToOriginProperties.payloads.push({
      packetCount,
      playerId: origin.currentlyOwnedByPlayerId,
      elapsedTicks: 0,
    });
  }
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
