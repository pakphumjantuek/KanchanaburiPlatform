<script setup lang="ts">
import type { District, ShopCategory } from '@/features/shops/api'

defineProps<{
  categories: ShopCategory[]
  districts: District[]
  activeFilterCount: number
  totalCount: number
}>()

const search = defineModel<string>('search', { default: '' })
const categoryId = defineModel<string | null>('categoryId', { default: null })
const districtId = defineModel<string | null>('districtId', { default: null })
const verifiedOnly = defineModel<boolean>('verifiedOnly', { default: false })
const viewMode = defineModel<'shops' | 'map'>('viewMode', { default: 'map' })

const emit = defineEmits<{
  (e: 'clear-filters'): void
  (e: 'filter-change'): void
}>()
</script>

<template>
  <div class="rounded-[1.75rem] bg-white border-2 border-[#E8D9C9] p-5 sm:p-6 shadow-md shadow-[#D96C2C]/5 space-y-4">
    <!-- Top Row: Title & View Mode Toggle & Active Count -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b-2 border-[#E8D9C9] pb-4">
      <div class="flex items-center gap-3">
        <div class="h-11 w-11 rounded-2xl bg-[#D96C2C] text-white flex items-center justify-center font-bold shadow-md">
          <i class="mdi mdi-map-search-outline text-2xl text-white"></i>
        </div>
        <div>
          <h2 class="text-lg sm:text-xl font-black text-[#332820] tracking-tight flex items-center gap-2">
            <span>แผนที่พิกัดร้านค้ากาญจนบุรี</span>
            <span class="text-xs font-bold text-[#D96C2C] bg-[#FFF9F2] border border-[#E8D9C9] px-3 py-0.5 rounded-full shadow-2xs">
              พบ {{ totalCount }} ร้านค้า
            </span>
          </h2>
          <p class="text-xs text-[#786B62] font-semibold">
            สำรวจตำแหน่งร้านค้าชุมชนและแหล่งผลิตสินค้าท้องถิ่นทั่วทั้งจังหวัด
          </p>
        </div>
      </div>

      <!-- Mode Switcher Buttons & Clear Filter -->
      <div class="flex items-center gap-3 self-start sm:self-auto">
        <button
          v-if="activeFilterCount"
          type="button"
          class="px-3 py-1 rounded-full bg-[#FFF9F2] border border-[#E8D9C9] text-xs font-bold text-[#D96C2C] hover:bg-[#D96C2C] hover:text-white transition cursor-pointer shadow-2xs"
          @click="emit('clear-filters')"
        >
          ล้างตัวกรองทั้งหมด
        </button>

        <div class="inline-flex rounded-full border-2 border-[#E8D9C9] bg-[#FFF9F2] p-1 shadow-2xs">
          <button
            type="button"
            class="flex items-center gap-1.5 px-4 py-1.5 rounded-full text-xs font-black transition cursor-pointer"
            :class="viewMode === 'shops' ? 'bg-[#D96C2C] text-white shadow-xs' : 'text-[#786B62] hover:text-[#332820]'"
            @click="viewMode = 'shops'"
          >
            <i class="mdi mdi-storefront-outline text-base"></i>
            <span>ร้านค้า</span>
          </button>
          <button
            type="button"
            class="flex items-center gap-1.5 px-4 py-1.5 rounded-full text-xs font-black transition cursor-pointer"
            :class="viewMode === 'map' ? 'bg-[#D96C2C] text-white shadow-xs' : 'text-[#786B62] hover:text-[#332820]'"
            @click="viewMode = 'map'"
          >
            <i class="mdi mdi-map-marker-radius text-base"></i>
            <span>แผนที่</span>
          </button>
        </div>
      </div>
    </div>

    <!-- Bottom Row: Filter Controls Moved To Top (Horizontal Bar) -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-12 gap-3 items-center">
      <!-- 1. Search Box (4 Cols) -->
      <form class="lg:col-span-4" role="search" @submit.prevent="emit('filter-change')">
        <div
          class="flex items-center gap-1.5 rounded-2xl border-2 border-[#E8D9C9] bg-white p-1 shadow-sm transition focus-within:border-[#D96C2C] focus-within:ring-4 focus-within:ring-[#D96C2C]/15"
        >
          <span class="flex h-8 w-8 shrink-0 items-center justify-center rounded-xl bg-[#FFF9F2] text-lg text-[#D96C2C]">
            <i class="mdi mdi-magnify"></i>
          </span>
          <input
            v-model="search"
            type="search"
            autocomplete="off"
            placeholder="ค้นหาชื่อร้านค้า / คำสำคัญ..."
            class="min-w-0 flex-1 bg-transparent py-1.5 text-xs font-bold text-[#332820] outline-none placeholder:font-medium placeholder:text-[#A8988C] [&::-webkit-search-cancel-button]:hidden"
          />
          <button
            v-if="search"
            type="button"
            class="flex h-7 w-7 shrink-0 cursor-pointer items-center justify-center rounded-full text-[#786B62] transition hover:bg-[#F7F0E6] hover:text-[#332820]"
            aria-label="ล้างคำค้นหา"
            @click="search = ''; emit('filter-change')"
          >
            <i class="mdi mdi-close text-base"></i>
          </button>
          <button
            type="submit"
            class="h-8 shrink-0 cursor-pointer rounded-xl bg-[#D96C2C] px-3 text-xs font-black text-white shadow-sm transition hover:bg-[#BF5720] active:scale-95"
          >
            ค้นหา
          </button>
        </div>
      </form>

      <!-- 2. Category Select (3 Cols) -->
      <div class="lg:col-span-3">
        <select
          v-model="categoryId"
          class="w-full rounded-full border-2 border-[#E8D9C9] bg-[#FFF9F2] px-4 py-2.5 text-xs font-bold text-[#332820] outline-none focus:border-[#D96C2C] focus:bg-white transition cursor-pointer shadow-2xs"
          @change="emit('filter-change')"
        >
          <option :value="null">หมวดหมู่ทั้งหมด</option>
          <option v-for="cat in categories" :key="cat.shopCategoryId" :value="cat.shopCategoryId">
            {{ cat.categoryName }}
          </option>
        </select>
      </div>

      <!-- 3. District Select (3 Cols) -->
      <div class="lg:col-span-3">
        <select
          v-model="districtId"
          class="w-full rounded-full border-2 border-[#E8D9C9] bg-[#FFF9F2] px-4 py-2.5 text-xs font-bold text-[#332820] outline-none focus:border-[#D96C2C] focus:bg-white transition cursor-pointer shadow-2xs"
          @change="emit('filter-change')"
        >
          <option :value="null">ทุกอำเภอในกาญจนบุรี</option>
          <option v-for="dist in districts" :key="dist.districtId" :value="dist.districtId">
            อ.{{ dist.districtName }}
          </option>
        </select>
      </div>

      <!-- 4. Quick Toggles (2 Cols) -->
      <div class="lg:col-span-2 flex items-center gap-3">
        <label class="flex items-center gap-1.5 text-xs font-bold text-[#332820] cursor-pointer shrink-0">
          <input v-model="verifiedOnly" type="checkbox" class="h-4 w-4 rounded accent-[#D96C2C]" />
          <span>รับรองแล้ว</span>
        </label>
      </div>
    </div>
  </div>
</template>
