<script setup lang="ts">
interface Props {
  search: string
  viewMode: 'contents' | 'map'
}

defineProps<Props>()

const emit = defineEmits<{
  'update:search': [value: string]
  'update:viewMode': [value: 'contents' | 'map']
  search: []
}>()

function onSearchInput(event: Event) {
  const target = event.target as HTMLInputElement
  emit('update:search', target.value)
}

function onSearchSubmit() {
  emit('search')
}

function clearSearch() {
  emit('update:search', '')
  emit('search')
}
</script>

<template>
  <header class="relative overflow-hidden border-b-2 border-[#E8D9C9] bg-[#F7F0E6] px-4 pt-8 pb-10 sm:px-6 sm:pt-10 sm:pb-12 lg:px-10">
    <!-- Decorative soft glows (brand colors, low opacity) -->
    <div class="pointer-events-none absolute -top-24 -right-16 h-72 w-72 rounded-full bg-[#F2A65A]/25 blur-3xl"></div>
    <div class="pointer-events-none absolute -bottom-28 left-1/4 h-64 w-64 rounded-full bg-[#D96C2C]/15 blur-3xl"></div>
    <div
      class="pointer-events-none absolute inset-0 opacity-[0.07]"
      style="background-image: radial-gradient(#332820 1px, transparent 1px); background-size: 22px 22px"
    ></div>

    <div class="relative mx-auto flex w-full max-w-[1800px] flex-col gap-6 sm:gap-8 md:flex-row md:items-end md:justify-between">
      <!-- Title & Branding -->
      <div class="max-w-2xl space-y-4">
        <div
          class="inline-flex items-center gap-2 rounded-full border border-[#E8D9C9] bg-white/80 px-3.5 py-1.5 text-xs font-bold text-[#D96C2C] shadow-2xs backdrop-blur"
        >
          <span class="flex h-5 w-5 items-center justify-center rounded-full bg-[#D96C2C] text-white">
            <i class="mdi mdi-compass-outline text-xs"></i>
          </span>
          <span>สำรวจเรื่องราวกาญจนบุรี</span>
        </div>
        <h1 class="text-2xl font-black leading-[1.35] tracking-tight text-[#332820] min-[400px]:text-3xl sm:text-4xl lg:text-5xl">
          เรื่องราวและการท่องเที่ยว<br />
          <span class="bg-gradient-to-r from-[#D96C2C] to-[#F2A65A] bg-clip-text pb-2 text-transparent">เมืองกาญจน์</span>
        </h1>
        <p class="max-w-xl text-sm font-medium leading-relaxed text-[#786B62] sm:text-base">
          เรื่องราว สถานที่ท่องเที่ยว วัฒนธรรม วิถีชีวิต และจุดเช็กอินน่าสนใจในกาญจนบุรี
        </p>
      </div>

      <!-- Search & View Mode Controls -->
      <div class="flex w-full flex-col gap-3 sm:flex-row sm:items-center md:w-auto">
        <!-- Search Input -->
        <form
          @submit.prevent="onSearchSubmit"
          class="group flex w-full items-center gap-2 rounded-full border-2 border-[#E8D9C9] bg-white py-1.5 pl-5 pr-1.5 shadow-md transition focus-within:border-[#D96C2C] focus-within:ring-4 focus-within:ring-[#D96C2C]/15 sm:w-80 lg:w-96"
        >
          <i class="mdi mdi-magnify text-lg text-[#786B62] transition group-focus-within:text-[#D96C2C]"></i>
          <input
            :value="search"
            @input="onSearchInput"
            type="text"
            placeholder="ค้นหาเรื่องราว, สถานที่..."
            class="min-w-0 flex-1 bg-transparent py-1.5 text-sm font-bold text-[#332820] focus:outline-none placeholder:font-medium placeholder:text-[#A8988C]"
            @keyup.enter="onSearchSubmit"
          />
          <button
            v-if="search"
            type="button"
            @click="clearSearch"
            class="cursor-pointer rounded-full p-1 text-[#786B62] transition hover:text-[#332820]"
            aria-label="ล้างคำค้นหา"
          >
            <i class="mdi mdi-close-circle text-base"></i>
          </button>
          <button
            type="submit"
            class="cursor-pointer select-none rounded-full bg-[#D96C2C] px-5 py-2 text-xs font-bold text-white shadow-sm transition hover:bg-[#BF5720] active:scale-95"
          >
            ค้นหา
          </button>
        </form>

        <!-- View Mode Segmented Control -->
        <div class="flex w-full shrink-0 items-center rounded-full border-2 border-[#E8D9C9] bg-white/80 p-1 shadow-2xs backdrop-blur sm:inline-flex sm:w-auto">
          <button
            type="button"
            class="flex flex-1 cursor-pointer select-none items-center justify-center gap-1.5 rounded-full px-3 py-2 text-xs font-bold outline-none transition-all duration-200 focus:outline-none sm:flex-none sm:px-4"
            :class="
              viewMode === 'contents'
                ? 'bg-[#D96C2C] text-white shadow-sm'
                : 'bg-transparent text-[#786B62] hover:text-[#332820]'
            "
            @click="emit('update:viewMode', 'contents')"
          >
            <i class="mdi mdi-play-box-multiple-outline text-sm"></i>
            <span>เรื่องราว / วิดีโอ</span>
          </button>
          <button
            type="button"
            class="flex flex-1 cursor-pointer select-none items-center justify-center gap-1.5 rounded-full px-3 py-2 text-xs font-bold outline-none transition-all duration-200 focus:outline-none sm:flex-none sm:px-4"
            :class="
              viewMode === 'map'
                ? 'bg-[#D96C2C] text-white shadow-sm'
                : 'bg-transparent text-[#786B62] hover:text-[#332820]'
            "
            @click="emit('update:viewMode', 'map')"
          >
            <i class="mdi mdi-map-marker-radius-outline text-sm"></i>
            <span>แผนที่ท่องเที่ยว</span>
          </button>
        </div>
      </div>
    </div>
  </header>
</template>
