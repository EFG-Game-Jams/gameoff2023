<script setup lang="ts">
import Edge from "@/components/Edge.vue";
import Node from "@/components/Node.vue";
import { EdgeType, EdgeTypes, GridNodeType, GridNodeTypes } from "@/types";
import { createEdge, createNode } from "@/factories";
import { useGridStore } from "@/stores/gridstore";
import { computed, onBeforeUnmount, onMounted, ref } from "vue";

const gridStore = useGridStore();
const toolModes = ["add nodes", "edge tool", "delete"] as const;
type ToolMode = (typeof toolModes)[number];
const ticksPerSecond = 10;

let idCounter: number = 0;
let tickIntervalId: number = 0;

const isRunning = ref<boolean>(false);
const toolMode = ref<ToolMode>(toolModes[0]);
const nodeTypeToCreate = ref<GridNodeType>(GridNodeTypes[0]);
const edgeTypeToCreate = ref<EdgeType>(EdgeTypes[0]);
const playerIdToUse = ref<null | number>(null);

const selection = ref<{ type: "node" | "edge"; id: number }[]>([]);

const onKeyPress = (ev: KeyboardEvent) => {
  switch (ev.key) {
    case "a":
      toolMode.value = "add nodes";
      break;

    case "d":
      toolMode.value = "delete";
      break;

    case "e":
      toolMode.value = "edge tool";
      break;

    case "0":
      playerIdToUse.value = null;
      break;

    case "1":
      playerIdToUse.value = 1;
      break;

    case "2":
      playerIdToUse.value = 2;
      break;

    case "3":
      playerIdToUse.value = 3;
      break;

    case "4":
      playerIdToUse.value = 4;
      break;
  }
};

const onScroll = (ev: WheelEvent) => {
  if (toolMode.value === "add nodes") {
    for (let i = 0; i < GridNodeTypes.length; ++i) {
      if (GridNodeTypes[i] == nodeTypeToCreate.value) {
        if (ev.deltaY > 0) {
          if (i + 1 === GridNodeTypes.length) {
            nodeTypeToCreate.value = GridNodeTypes[0];
          } else {
            nodeTypeToCreate.value = GridNodeTypes[i + 1];
          }
        } else {
          if (i - 1 < 0) {
            nodeTypeToCreate.value = GridNodeTypes[GridNodeTypes.length - 1];
          } else {
            nodeTypeToCreate.value = GridNodeTypes[i - 1];
          }
        }
        return;
      }
    }
  }
};

onMounted(() => {
  document.addEventListener("keypress", onKeyPress);
  document.addEventListener("wheel", onScroll);
});

onBeforeUnmount(() => {
  document.removeEventListener("keypress", onKeyPress);
  document.removeEventListener("wheel", onScroll);
});

const onGridClick = (ev: MouseEvent) => {
  console.debug("grid clicked");

  switch (toolMode.value) {
    case "add nodes":
      const toAdd = createNode(
        nodeTypeToCreate.value,
        idCounter++,
        ev.clientX,
        ev.clientY,
        playerIdToUse.value
      );
      console.debug(`Adding node with ID ${toAdd.id}`, toAdd);
      gridStore.nodes.push(toAdd);
      break;

    case "edge tool":
      selection.value = [];
      break;

    case "delete":
      break;
  }
};

const onNodeClick = (id: number) => {
  console.debug("node clicked");

  switch (toolMode.value) {
    case "add nodes":
      break;

    case "edge tool":
      if (selection.value.length > 1) {
        console.debug(`Node with ID ${id} selected`);
        selection.value = [{ id, type: "node" }];
      } else if (selection.value.length === 1) {
        if (selection.value[0].id !== id) {
          console.debug(`Node with ID ${id} selected`);
          selection.value = [{ id, type: "node" }];
        } else {
          console.debug(`Node with ID ${id} deselected`);
          selection.value = [];
        }
      } else {
        console.debug(`Node with ID ${id} selected`);
        selection.value = [{ id, type: "node" }];
      }
      break;

    case "delete":
      console.debug(`Deleting node with ID ${id}`);
      const index = gridStore.nodes.findIndex((n) => n.id == id);
      if (index >= 0) {
        gridStore.edges = gridStore.edges.filter(
          (e) => e.originNodeId != id && e.destinationNodeId != id
        );
        gridStore.nodes.splice(index, 1);
      }
      break;
  }
};

const onNodeShiftClick = (id: number) => {
  console.debug("node shift clicked");
  switch (toolMode.value) {
    case "add nodes":
      break;

    case "edge tool":
      const existingSelectIndex = selection.value.findIndex((v) => v.id === id);
      if (existingSelectIndex < 0) {
        console.debug(`Node with ID ${id} added to the selection`);
        selection.value = [
          ...selection.value,
          {
            id: id,
            type: "node",
          },
        ];
      } else {
        console.debug(`Node with ID ${id} removed from the selection`);
        selection.value.splice(existingSelectIndex, 1);
      }

      break;

    case "delete":
      break;
  }
};

const onEdgeClick = (id: number) => {
  console.debug("edge clicked");

  switch (toolMode.value) {
    case "add nodes":
      break;

    case "edge tool":
      selection.value = [];
      break;

    case "delete":
      console.debug(`Deleting edge with ID ${id}`);
      const index = gridStore.edges.findIndex((n) => n.id == id);
      if (index >= 0) {
        gridStore.edges.splice(index, 1);
      }
      break;
  }
};

const onRun = () => {
  isRunning.value = true;
  tickIntervalId = setInterval(gridStore.tick, 1000 / ticksPerSecond);
};

const onStop = () => {
  clearInterval(tickIntervalId);
  isRunning.value = false;
};

const canCreateEdge = computed<boolean>(() => {
  return (
    selection.value.length == 2 &&
    selection.value.every((v) => v.type == "node")
  );
});
const onCreateEdge = () => {
  const candidateEdge = createEdge(
    "normal",
    idCounter++,
    selection.value[0].id,
    selection.value[1].id
  );
  if (
    gridStore.edges.some(
      (e) =>
        e.originNodeId === candidateEdge.originNodeId &&
        e.destinationNodeId === candidateEdge.destinationNodeId
    ) ||
    gridStore.edges.some(
      (e) =>
        e.originNodeId === candidateEdge.destinationNodeId &&
        e.destinationNodeId === candidateEdge.originNodeId
    )
  ) {
    console.error("Edge already exists, ignoring");
  } else {
    const originNode = gridStore.nodes.find(
      (n) => n.id === candidateEdge.originNodeId
    );
    if (originNode?.type === "uplink") {
      candidateEdge.destinationToOriginProperties.enabled = false;
    }
    gridStore.edges.push(candidateEdge);
  }

  selection.value = [];
};

const errors = computed<string[]>(() => {
  const messages = [];

  const servers = gridStore.nodes.filter((n) => n.type === "server");
  if (servers.some((s) => s.currentlyOwnedByPlayerId == null)) {
    messages.push("Server(s) lack(s) owner(s)");
  } else if (
    servers.some(
      (s) => !gridStore.players.some((p) => p.id === s.currentlyOwnedByPlayerId)
    )
  ) {
    messages.push("Some servers are owned by nonexisting player");
  }

  if (gridStore.players.length < 1) {
    messages.push("One or more players are required");
  } else if (
    new Set(
      servers
        .filter((s) => s.currentlyOwnedByPlayerId != null)
        .map((s) => s.currentlyOwnedByPlayerId)
    ).size < 2
  ) {
    messages.push("Two or more players are required");
  }

  // TODO basic reachability graph analysis (server to nexus)

  return messages;
});
</script>

<template>
  <div class="viewport">
    <div class="grid" @click.exact="onGridClick($event)">
      <Edge
        v-for="edge in gridStore.edges"
        @click.stop.exact="onEdgeClick(edge.id)"
        :key="edge.id"
        :edge="edge"
        :selected="selection.some((s) => s.id == edge.id)"
      />
      <Node
        v-for="node in gridStore.nodes"
        @click.stop.exact="onNodeClick(node.id)"
        @click.shift.stop.exact="onNodeShiftClick(node.id)"
        :key="node.id"
        :node="node"
        :selected="selection.some((s) => s.id == node.id)"
      />
    </div>

    <div class="controls">
      <label> Node count {{ gridStore.nodes.length }} </label>
      <label v-if="toolMode === 'edge tool'">
        Selection count {{ selection.length }}
      </label>
      <div style="color: darkgray; font-size: 11pt">
        (A)dd nodes <br />
        <ul style="margin: 0px">
          <li>Left click to place</li>
          <li>Scroll to change node type</li>
          <li>0-4 Set player ID (null, 1, 2, ...)</li>
        </ul>
        (E)dge tool <br />
        <ul style="margin: 0px">
          <li>Shift left click to select multiple nodes</li>
        </ul>
        (D)elete tool <br />
        <ul style="margin: 0px">
          <li>Left click to delete</li>
        </ul>
      </div>
      <hr style="width: 100%" />

      <label>
        Tool type
        <select v-model="toolMode">
          <option v-for="mode in toolModes" :value="mode">
            {{ mode }}
          </option>
        </select>
      </label>

      <template v-if="toolMode === 'edge tool'">
        <label>
          Edge type
          <select v-model="edgeTypeToCreate">
            <option v-for="edgeType in EdgeTypes" :value="edgeType">
              {{ edgeType }}
            </option>
          </select>
        </label>

        <button
          :disabled="!canCreateEdge"
          :title="!canCreateEdge ? 'select two nodes first' : ''"
          @click="onCreateEdge"
        >
          Create edge
        </button>
      </template>

      <template v-if="toolMode === 'add nodes'">
        <label>
          Node type
          <select v-model="nodeTypeToCreate">
            <option v-for="nodeType in GridNodeTypes" :value="nodeType">
              {{ nodeType }}
            </option>
          </select>
        </label>

        <label>
          Player ID
          <select v-model="playerIdToUse">
            <option value="null">None</option>
            <option v-for="player in gridStore.players" :value="player.id">
              {{ player.id }}
            </option>
          </select>
        </label>
      </template>

      <hr style="width: 100%" />

      <p style="color: red" v-for="error in errors">{{ error }}</p>

      <template v-if="!isRunning">
        <button
          :disabled="errors.length > 0"
          :title="errors.length > 0 ? 'Fix the errors first' : ''"
          @click="onRun"
        >
          Run
        </button>
        <button @click="gridStore.resetPlayerProgression()">Reset</button>
        <button @click="gridStore.clear()">Clear</button>
      </template>

      <template v-else>
        <button @click="onStop">Stop</button>
      </template>
    </div>
  </div>
</template>

<style scoped>
.viewport {
  display: flex;
  flex-wrap: nowrap;
}

.viewport > *:not(:first-child) {
  margin-left: 10px;
}

.grid {
  position: relative;
  width: 800px;
  height: 800px;
  border: 1px solid red;
}

.controls {
  display: flex;
  flex-direction: column;
  background-color: white;
  border-radius: 5px;
  padding: 10px;
}

.controls > label:not(:first-child),
.controls > button:not(:first-child) {
  margin-top: 10px;
}
</style>
