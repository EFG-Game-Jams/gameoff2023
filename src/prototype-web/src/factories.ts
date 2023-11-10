import {
  DefaultFlowMode,
  EdgeType,
  GridNodeType,
  IEdge,
  IGridNode,
  IPlayer,
} from "@/types";

const defaultNodePacketCapacity = 100000;
const defaultEdgeBandwidthInPacketsPerTick = 200;
const defaultEdgeTransferLatencyInTicks = 2;
const defaultServerPacketProductionPerTick = 10;

export function createNode(
  type: GridNodeType,
  id: number,
  x: number,
  y: number,
  ownedByplayerId: number | null
): IGridNode {
  const packetProductionPerTick =
    type === "server" ? defaultServerPacketProductionPerTick : 0;
  return {
    type: type,
    id,
    position: { x, y },
    spawnOwnedByPlayerId: ownedByplayerId,
    currentlyOwnedByPlayerId: ownedByplayerId,
    playerProperties:
      ownedByplayerId == null
        ? []
        : [
            {
              playerId: ownedByplayerId,
              intendedFillRatio: 0.5,
              disableInflow: false,
            },
          ],
    capacityLimit: defaultNodePacketCapacity,
    capacityUsed: 0,
    packetProductionPerTick,
    appliedUpgrades: [],
  };
}

export function createEdge(
  type: EdgeType,
  id: number,
  originNodeId: number,
  destinationNodeId: number
): IEdge {
  return {
    type,
    id,
    originNodeId,
    destinationNodeId,
    originToDestinationProperties: {
      enabled: true,
      bandwidthInPacketsPerTick: defaultEdgeBandwidthInPacketsPerTick,
      transferLatencyInTicks: defaultEdgeTransferLatencyInTicks,
      payloads: [],
    },
    destinationToOriginProperties: {
      enabled: true,
      bandwidthInPacketsPerTick: defaultEdgeBandwidthInPacketsPerTick,
      transferLatencyInTicks: defaultEdgeTransferLatencyInTicks,
      payloads: [],
    },
  };
}

export function createPlayer(
  id: number,
  defaultFlowMode: DefaultFlowMode
): IPlayer {
  return {
    id,
    defaultFlowMode,
  };
}
