<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'

// Counts up from 0 (or the previous value) to `value`. Shows the final number at once for reduced motion.
const props = withDefaults(defineProps<{ value: number; duration?: number }>(), { duration: 1400 })

const display = ref(0)
let frame = 0

function animateTo(target: number) {
  cancelAnimationFrame(frame)
  const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches
  if (reduce || !Number.isFinite(target)) {
    display.value = target
    return
  }
  const from = display.value
  const start = performance.now()
  const tick = (now: number) => {
    const t = Math.min(1, (now - start) / props.duration)
    const eased = 1 - Math.pow(1 - t, 3) // ease-out
    display.value = Math.round(from + (target - from) * eased)
    if (t < 1) frame = requestAnimationFrame(tick)
  }
  frame = requestAnimationFrame(tick)
}

onMounted(() => animateTo(props.value))
watch(() => props.value, animateTo)
onBeforeUnmount(() => cancelAnimationFrame(frame))
</script>

<template>
  <span class="tabular-nums">{{ display.toLocaleString('th-TH') }}</span>
</template>