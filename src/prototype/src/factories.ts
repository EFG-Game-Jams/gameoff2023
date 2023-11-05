import {
  DefaultFlowMode,
  EdgeType,
  GridNodeType,
  IEdge,
  IGridNode,
  IPlayer,
} from "@/types";

const defaultNodePacketCapacity = 1000;
const defaultEdgeBandwidthInPacketsPerTick = 2;
const defaultEdgeTransferLatencyInTicks = 1;
const defaultServerPacketProductionPerTick = 10;

export function createNode(
  type: GridNodeType,
  id: number,
  x: number,
  y: number,
  playerId: number | null
): IGridNode {
  const packetProductionPerTick =
    type === "server" ? defaultServerPacketProductionPerTick : 0;
  return {
    type: type,
    id,
    position: { x, y },
    spawnOwnedByPlayerId: playerId,
    currentlyOwnedByPlayerId: playerId,
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
