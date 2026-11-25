<script setup lang="ts">
interface Props {
  icon?: string
  title?: string
  description?: string
  actionLabel?: string
}

withDefaults(defineProps<Props>(), {
  icon: 'mdi-play-box-remove-outline',
  title: 'ไม่พบข้อมูล',
  description: 'ลองปรับเปลี่ยนคำค้นหาหรือตัวกรองหมวดหมู่ เพื่อค้นหาข้อมูลที่คุณสนใจ',
  actionLabel: 'ล้างตัวกรองทั้งหมด',
})

const emit = defineEmits<{
  (e: 'action'): void
}>()
</script>

<template>
  <div
    class="rounded-3xl border-2 border-[#E8D9C9] bg-white p-12 text-center shadow-xs space-y-4"
  >
    <div
      class="h-16 w-16 rounded-full bg-[#FFF9F2] text-[#D96C2C] flex items-center justify-center mx-auto text-3xl font-bold border border-[#E8D9C9]"
    >
      <i :class="['mdi', icon]" />
    </div>
    <h3 class="text-lg font-black text-[#332820]">{{ title }}</h3>
    <p v-if="description" class="text-xs text-[#786B62] max-w-sm mx-auto font-bold leading-relaxed">
      {{ description }}
    </p>
    <button
      v-if="actionLabel"
      type="button"
      class="px-6 py-2.5 rounded-full bg-[#D96C2C] hover:bg-[#BF5720] text-white text-xs font-bold transition shadow-md cursor-pointer border border-[#D96C2C] select-none active:scale-95 outline-none focus:outline-none"
      @click="emit('action')"
    >
      {{ actionLabel }}
    </button>
  </div>
</template>
