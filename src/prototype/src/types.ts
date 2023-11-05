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
  spawnOwnedByPlayerId: null | number;
  currentlyOwnedByPlayerId: null | number;
  capacityLimit: number;
  capacityUsed: number;
  packetProductionPerTick: number;
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
  bandwidthInPacketsPerTick: number;
  transferLatencyInTicks: number;
  payloads: IPacketsInTransit[];
}

export interface IPacketsInTransit {
  playerId: number;
  packetCount: number;
  elapsedTicks: number;
}

export const DefaultFlowModes = [
  "frontline",
  "outward",
  "overflow",
  "none",
] as const;
export type DefaultFlowMode = (typeof DefaultFlowModes)[number];

export interface IPlayer {
  id: number;
  defaultFlowMode: DefaultFlowMode;
}
