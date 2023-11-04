import { defineStore } from "pinia";
import { IEdge, IGridNode, IPlayer } from "@/types";

type StoreState = {
  nodes: IGridNode[];
  edges: IEdge[];
  players: IPlayer[];
};

export const useGridStore = defineStore("grid-store", {
  state: (): StoreState => ({
    nodes: [],
    edges: [],
    players: [],
  }),
  actions: {
    reset() {
      this.nodes = [];
      this.edges = [];
      this.players = [];
    },
    tick() {},
  },
  getters: {},
});
