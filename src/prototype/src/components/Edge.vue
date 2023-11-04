<script setup lang="ts">
import { useGridStore } from "@/stores/gridstore";
import { IEdge, IPosition } from "@/types";
import { StyleValue, computed } from "vue";

const gridStore = useGridStore();

const props = defineProps<{
  edge: IEdge;
  selected: boolean;
}>();

const points = computed<IPosition[]>(() => {
  return [
    gridStore.nodes.find((n) => n.id === props.edge.originNodeId)?.position ?? {
      x: 0,
      y: 0,
    },
    gridStore.nodes.find((n) => n.id === props.edge.destinationNodeId)
      ?.position ?? { x: 0, y: 0 },
  ];
});

const edgeStyle = computed((): StyleValue => {
  const origin = points.value[0];
  const destination = points.value[1];

  // Get distance between the points for length of line
  const diffX = origin.x - destination.x;
  const diffY = origin.y - destination.y;
  const length = Math.sqrt(diffX * diffX + diffY * diffY);

  // Get angle between points
  const rotation =
    (Math.atan2(destination.y - origin.y, destination.x - origin.x) * 180) /
    Math.PI;

  return {
    // Set line distance and position
    // Add width/height from above so the line starts in the middle instead of the top-left corner
    width: length + "px",
    left: origin.x - 25 + "px",
    top: origin.y - 25 + "px",
    // Rotate line to match angle between points
    transform: "rotate(" + rotation + "deg)",
    borderWidth: props.selected ? "1px" : "0px",
  };
});
</script>

<template>
  <div class="edge" :style="edgeStyle">
    <span
      v-if="!edge.destinationToOriginProperties.enabled"
      class="chevron right"
    ></span>
  </div>
</template>

<style scoped>
div.edge {
  position: absolute;
  display: flex;
  justify-content: center;
  align-items: center;
  align-content: center;
  z-index: 10;
  height: 2px;
  background-color: grey;
  transform-origin: left;
  border-color: yellow;
  border-style: solid;
}

.chevron::before {
  border-style: solid;
  border-color: grey;
  border-width: 0.25em 0.25em 0 0;
  content: "";
  display: inline-block;
  height: 10px;
  width: 10px;
  left: 5px;
  position: relative;
  top: 4px;
  transform: rotate(-45deg);
  vertical-align: top;
}

.chevron.right:before {
  left: 0;
  transform: rotate(45deg);
}

.chevron.left:before {
  left: 0.25em;
  transform: rotate(-135deg);
}
</style>
