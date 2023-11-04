export const GridNodeTypes = [
  "storage",
  "switch",
  "server",
  "uplink",
  "hub",
  "nexus",
] as const;
export type GridNodeType = (typeof GridNodeTypes)[number];

export const EdgeTypes = ["normal", "long distance"] as const;
export type EdgeType = (typeof EdgeTypes)[number];

export interface IPosition {
  x: number;
  y: number;
}

export interface IGridNode {
  type: GridNodeType;
  id: number;
  position: IPosition;
  ownedByPlayerId: null | number;
  capacity: number;
  production: number;
  appliedUpgrades: string[];
}

export interface INexusNode extends IGridNode {
  type: "nexus";
  captureProgress: [{ playerId: number; packets: number }];
}

export interface IUplinkNode extends IGridNode {
  type: "uplink";
  range: number;
}

export interface IEdge {
  type: EdgeType;
  id: number;
  originNodeId: number;
  destinationNodeId: number;
  originToDestinationProperties: IEdgeDirectionProperties;
  destinationToOriginProperties: IEdgeDirectionProperties;
}

export interface IEdgeDirectionProperties {
  enabled: boolean;
  bandwidthInPacketsPerSecond: number;
  latencyInSeconds: number;
  inTransit: { packetCount: number; secondsRemaining: number }[];
}

export interface IPlayer {
  id: number;
}
