import { EdgeType, GridNodeType, IEdge, IGridNode } from "@/types";

const defaultNodePacketCapacity = 100;
const defaultEdgeBandwidthInPacketsPerSecond = 10;
const defaultEdgeTransferLatencyInSeconds = 0.5;

export function createNode(
  type: GridNodeType,
  id: number,
  x: number,
  y: number,
  playerId: number | null
): IGridNode {
  return {
    type: type,
    id,
    position: { x, y },
    ownedByPlayerId: playerId,
    capacity: defaultNodePacketCapacity,
    production: 0,
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
      bandwidthInPacketsPerSecond: defaultEdgeBandwidthInPacketsPerSecond,
      latencyInSeconds: defaultEdgeTransferLatencyInSeconds,
      inTransit: [],
    },
    destinationToOriginProperties: {
      enabled: true,
      bandwidthInPacketsPerSecond: defaultEdgeBandwidthInPacketsPerSecond,
      latencyInSeconds: defaultEdgeTransferLatencyInSeconds,
      inTransit: [],
    },
  };
}
