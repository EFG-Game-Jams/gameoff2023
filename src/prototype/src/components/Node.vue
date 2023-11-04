<script setup lang="ts">
import { IGridNode } from "@/types";
import { StyleValue, computed } from "vue";

const props = defineProps<{
  node: IGridNode;
  selected: boolean;
}>();

const nodeWidthInPixels = 50;
const nodeHeightInPixels = 50;

const nodeColor = computed((): string => {
  if (props.node.ownedByPlayerId === 1) {
    return "green";
  } else if (props.node.ownedByPlayerId != null) {
    return "red";
  }

  return "white";
});
const edgeStyle = computed((): StyleValue => {
  return {
    position: "absolute",
    top: `${props.node.position.y - nodeHeightInPixels}px`,
    left: `${props.node.position.x - nodeWidthInPixels}px`,
    width: `${nodeWidthInPixels}px`,
    height: `${nodeHeightInPixels}px`,
    backgroundColor: nodeColor.value,
    borderWidth: props.selected ? "1px" : "0px",
    borderColor: "yellow",
    borderStyle: "solid",
  };
});
</script>

<template>
  <div :style="edgeStyle">
    <p>
      {{ props.node.type }}
    </p>
  </div>
</template>

<style scoped>
div {
  user-select: none;
  font-size: 8pt;
  display: flex;
  justify-content: center;
  align-items: center;
  border-radius: 4px;
  z-index: 100;
}
</style>
