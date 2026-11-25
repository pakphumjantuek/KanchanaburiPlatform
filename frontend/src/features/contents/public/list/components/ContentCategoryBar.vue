<script setup lang="ts">
import type { ContentCategory } from '../../api/contentApi'

interface Props {
  categories: ContentCategory[]
  selectedCategoryId: string | null
}

defineProps<Props>()

const emit = defineEmits<{
  select: [categoryId: string | null]
}>()
</script>

<template>
  <section class="sticky top-16 z-20 border-b sm:top-20 border-[#E8D9C9]/80 bg-[#F7F0E6]/85 shadow-sm shadow-[#332820]/5 backdrop-blur-md">
    <div
      class="mx-auto flex w-full max-w-[1800px] items-center gap-2.5 overflow-x-auto px-4 py-3 sm:px-6 lg:px-10 scrollbar-none"
    >
      <!-- All Categories Capsule Pill -->
      <button
        type="button"
        class="shrink-0 rounded-full border-2 px-4 py-2 text-xs sm:text-sm font-bold transition-all duration-200 cursor-pointer flex items-center gap-2 select-none active:scale-95 shadow-2xs outline-none focus:outline-none"
        :class="
          selectedCategoryId === null
            ? 'border-[#D96C2C] bg-[#D96C2C] text-white shadow-sm'
            : 'border-[#E8D9C9] bg-white text-[#332820] hover:border-[#D96C2C]'
        "
        @click="emit('select', null)"
      >
        <span
          class="flex h-5 w-5 items-center justify-center rounded-full text-xs"
          :class="selectedCategoryId === null ? 'bg-white/20 text-white' : 'bg-[#F7F0E6] text-[#786B62]'"
        >
          <i class="mdi mdi-compass-outline"></i>
        </span>
        <span>ทั้งหมด</span>
      </button>

      <!-- Category Pills -->
      <button
        v-for="cat in categories"
        :key="cat.contentCategoryId"
        type="button"
        class="shrink-0 rounded-full border-2 px-4 py-2 text-xs sm:text-sm font-bold transition-all duration-200 cursor-pointer flex items-center gap-2 select-none active:scale-95 shadow-2xs outline-none focus:outline-none"
        :class="
          selectedCategoryId === cat.contentCategoryId
            ? 'border-[#D96C2C] bg-[#D96C2C] text-white shadow-sm'
            : 'border-[#E8D9C9] bg-white text-[#332820] hover:border-[#D96C2C]'
        "
        @click="emit('select', cat.contentCategoryId)"
      >
        <span
          class="flex h-5 w-5 items-center justify-center rounded-full text-xs"
          :class="selectedCategoryId === cat.contentCategoryId ? 'bg-white/20 text-white' : 'bg-[#F7F0E6] text-[#786B62]'"
        >
          <i class="mdi mdi-tag-outline"></i>
        </span>
        <span>{{ cat.categoryName }}</span>
      </button>
    </div>
  </section>
</template>
