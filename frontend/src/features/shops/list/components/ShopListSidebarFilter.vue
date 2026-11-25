<script setup lang="ts">
import { onBeforeUnmount } from 'vue'
import type { District, ShopCategory } from '@/features/shops/api'

defineProps<{
  categories: ShopCategory[]
  districts: District[]
  activeFilterCount: number
}>()

const search = defineModel<string>('search', { default: '' })
const categoryId = defineModel<string | null>('categoryId', { default: null })
const districtId = defineModel<string | null>('districtId', { default: null })
const verifiedOnly = defineModel<boolean>('verifiedOnly', { default: false })

const emit = defineEmits<{
  (e: 'clear-filters'): void
  (e: 'filter-change'): void
}>()

// Search applies itself shortly after typing stops (Enter still applies immediately)
let searchTimer: ReturnType<typeof setTimeout> | undefined
function onSearchInput() {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => emit('filter-change'), 450)
}

function applySearchNow() {
  clearTimeout(searchTimer)
  emit('filter-change')
}

function clearSearch() {
  search.value = ''
  applySearchNow()
}

onBeforeUnmount(() => clearTimeout(searchTimer))
</script>

<template>
  <aside>
    <div
      class="space-y-6 rounded-[1.75rem] border-2 border-[#E8D9C9] bg-white p-5 shadow-md shadow-[#D96C2C]/5 sm:p-6"
    >
      <!-- Header -->
      <div class="flex items-center justify-between gap-3">
        <div class="flex items-center gap-3">
          <div
            class="relative flex h-10 w-10 items-center justify-center rounded-xl bg-[#D96C2C] text-white shadow-md"
          >
            <i class="mdi mdi-tune-variant text-lg"></i>
            <span
              v-if="activeFilterCount"
              class="absolute -top-1.5 -right-1.5 flex h-5 min-w-5 items-center justify-center rounded-full border-2 border-white bg-[#332820] px-1 text-[10px] font-black text-white"
            >
              {{ activeFilterCount }}
            </span>
          </div>
          <div>
            <h3 class="text-base font-black text-[#332820] sm:text-lg">ตัวกรองร้านค้า</h3>
            <p class="text-[11px] font-semibold text-[#786B62]">
              {{ activeFilterCount ? `เลือกอยู่ ${activeFilterCount} รายการ` : 'ค้นหาตามหมวดและพื้นที่' }}
            </p>
          </div>
        </div>

        <button
          v-if="activeFilterCount || verifiedOnly"
          type="button"
          class="flex cursor-pointer items-center gap-1 rounded-full border border-[#E8D9C9] bg-[#FFF9F2] px-3 py-1.5 text-xs font-bold text-[#D96C2C] shadow-2xs transition hover:bg-[#D96C2C] hover:text-white focus-visible:ring-2 focus-visible:ring-[#D96C2C]/40 focus-visible:outline-none active:scale-95"
          @click="emit('clear-filters')"
        >
          <i class="mdi mdi-refresh text-sm"></i>
          <span>ล้าง</span>
        </button>
      </div>

      <!-- 1. Search -->
      <form class="space-y-2" role="search" @submit.prevent="applySearchNow">
        <label for="shop-filter-search" class="block text-xs font-black text-[#332820] sm:text-sm">
          ค้นหาร้านค้า
        </label>
        <div
          class="flex items-center gap-1.5 rounded-2xl border-2 border-[#E8D9C9] bg-white p-1.5 shadow-sm transition focus-within:border-[#D96C2C] focus-within:shadow-md focus-within:ring-4 focus-within:ring-[#D96C2C]/15"
        >
          <span
            class="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-[#FFF9F2] text-lg text-[#D96C2C]"
          >
            <i class="mdi mdi-magnify"></i>
          </span>
          <input
            id="shop-filter-search"
            v-model="search"
            type="search"
            autocomplete="off"
            placeholder="ชื่อร้าน, หัตถกรรม, อาหาร..."
            class="min-w-0 flex-1 bg-transparent py-2 text-sm font-bold text-[#332820] outline-none placeholder:font-medium placeholder:text-[#A8988C] [&::-webkit-search-cancel-button]:hidden"
            @input="onSearchInput"
          />
          <button
            v-if="search"
            type="button"
            class="flex h-7 w-7 shrink-0 cursor-pointer items-center justify-center rounded-full text-[#786B62] transition hover:bg-[#F7F0E6] hover:text-[#332820]"
            aria-label="ล้างคำค้นหา"
            @click="clearSearch"
          >
            <i class="mdi mdi-close text-base"></i>
          </button>
          <button
            type="submit"
            class="h-9 shrink-0 cursor-pointer rounded-xl bg-[#D96C2C] px-3.5 text-xs font-black text-white shadow-sm transition hover:bg-[#BF5720] active:scale-95"
          >
            ค้นหา
          </button>
        </div>
      </form>

      <!-- 2. Verified toggle (switch) -->
      <button
        type="button"
        role="switch"
        :aria-checked="verifiedOnly"
        class="flex w-full cursor-pointer items-center justify-between gap-3 rounded-2xl border-2 px-4 py-3 text-left transition focus-visible:ring-4 focus-visible:ring-[#D96C2C]/20 focus-visible:outline-none"
        :class="
          verifiedOnly
            ? 'border-[#D96C2C] bg-[#FFF9F2]'
            : 'border-[#E8D9C9] bg-white hover:border-[#D96C2C]/60'
        "
        @click="verifiedOnly = !verifiedOnly"
      >
        <span class="flex items-center gap-2.5">
          <span
            class="flex h-8 w-8 items-center justify-center rounded-full transition"
            :class="verifiedOnly ? 'bg-[#D96C2C] text-white' : 'bg-[#F7F0E6] text-[#D96C2C]'"
          >
            <i class="mdi mdi-shield-check text-base"></i>
          </span>
          <span>
            <span class="block text-xs font-black text-[#332820] sm:text-sm">เฉพาะร้านที่ผ่านการรับรอง</span>
            <span class="block text-[11px] font-semibold text-[#786B62]">ซ่อนร้านที่ยังไม่ได้รับรอง</span>
          </span>
        </span>
        <span
          class="relative h-6 w-11 shrink-0 rounded-full transition-colors"
          :class="verifiedOnly ? 'bg-[#D96C2C]' : 'bg-[#E8D9C9]'"
        >
          <span
            class="absolute top-0.5 left-0.5 h-5 w-5 rounded-full bg-white shadow transition-transform"
            :class="verifiedOnly ? 'translate-x-5' : ''"
          ></span>
        </span>
      </button>

      <!-- 3. Categories -->
      <fieldset class="space-y-2.5 border-t-2 border-[#E8D9C9] pt-5">
        <legend class="mb-1 flex w-full items-center justify-between text-xs font-black text-[#332820] sm:text-sm">
          <span>หมวดหมู่ร้านค้า</span>
          <span class="text-[11px] font-bold text-[#786B62]">{{ categories.length }} หมวด</span>
        </legend>
        <div class="scrollbar-thin max-h-60 space-y-1.5 overflow-y-auto pr-1">
          <label
            class="flex cursor-pointer items-center justify-between rounded-xl border-2 px-3.5 py-2 text-xs font-bold transition has-[:focus-visible]:ring-4 has-[:focus-visible]:ring-[#D96C2C]/20 sm:text-sm"
            :class="
              categoryId === null
                ? 'border-[#D96C2C] bg-[#FFF9F2] text-[#D96C2C]'
                : 'border-transparent bg-[#F7F0E6]/60 text-[#332820] hover:border-[#E8D9C9] hover:bg-white'
            "
          >
            <input
              type="radio"
              name="category_filter"
              class="sr-only"
              :checked="categoryId === null"
              @change="categoryId = null"
            />
            <span class="flex items-center gap-2">
              <i class="mdi mdi-view-grid-outline text-base"></i>
              <span>หมวดหมู่ทั้งหมด</span>
            </span>
            <i v-if="categoryId === null" class="mdi mdi-check-circle text-base"></i>
          </label>

          <label
            v-for="cat in categories"
            :key="cat.shopCategoryId"
            class="flex cursor-pointer items-center justify-between rounded-xl border-2 px-3.5 py-2 text-xs font-bold transition has-[:focus-visible]:ring-4 has-[:focus-visible]:ring-[#D96C2C]/20 sm:text-sm"
            :class="
              categoryId === cat.shopCategoryId
                ? 'border-[#D96C2C] bg-[#FFF9F2] text-[#D96C2C]'
                : 'border-transparent bg-[#F7F0E6]/60 text-[#332820] hover:border-[#E8D9C9] hover:bg-white'
            "
          >
            <input
              type="radio"
              name="category_filter"
              class="sr-only"
              :checked="categoryId === cat.shopCategoryId"
              @change="categoryId = cat.shopCategoryId"
            />
            <span class="flex min-w-0 items-center gap-2">
              <i class="mdi mdi-tag-outline text-base"></i>
              <span class="truncate">{{ cat.categoryName }}</span>
            </span>
            <i v-if="categoryId === cat.shopCategoryId" class="mdi mdi-check-circle text-base"></i>
          </label>
        </div>
      </fieldset>

      <!-- 4. Districts (chips) -->
      <fieldset class="space-y-2.5 border-t-2 border-[#E8D9C9] pt-5">
        <legend class="mb-1 flex w-full items-center justify-between text-xs font-black text-[#332820] sm:text-sm">
          <span>พื้นที่อำเภอ</span>
          <button
            v-if="districtId"
            type="button"
            class="cursor-pointer text-[11px] font-bold text-[#D96C2C] hover:underline"
            @click="districtId = null"
          >
            ล้าง
          </button>
        </legend>
        <div class="scrollbar-thin flex max-h-48 flex-wrap gap-1.5 overflow-y-auto pr-1">
          <label
            class="cursor-pointer rounded-full border-2 px-3 py-1.5 text-xs font-bold transition has-[:focus-visible]:ring-4 has-[:focus-visible]:ring-[#D96C2C]/20"
            :class="
              districtId === null
                ? 'border-[#D96C2C] bg-[#D96C2C] text-white shadow-sm'
                : 'border-[#E8D9C9] bg-white text-[#332820] hover:border-[#D96C2C]'
            "
          >
            <input
              type="radio"
              name="district_filter"
              class="sr-only"
              :checked="districtId === null"
              @change="districtId = null"
            />
            ทุกอำเภอ
          </label>

          <label
            v-for="dist in districts"
            :key="dist.districtId"
            class="cursor-pointer rounded-full border-2 px-3 py-1.5 text-xs font-bold transition has-[:focus-visible]:ring-4 has-[:focus-visible]:ring-[#D96C2C]/20"
            :class="
              districtId === dist.districtId
                ? 'border-[#D96C2C] bg-[#D96C2C] text-white shadow-sm'
                : 'border-[#E8D9C9] bg-white text-[#332820] hover:border-[#D96C2C]'
            "
          >
            <input
              type="radio"
              name="district_filter"
              class="sr-only"
              :checked="districtId === dist.districtId"
              @change="districtId = dist.districtId"
            />
            อ.{{ dist.districtName }}
          </label>
        </div>
      </fieldset>
    </div>
  </aside>
</template>

<style scoped>
.scrollbar-thin::-webkit-scrollbar {
  width: 4px;
}
.scrollbar-thin::-webkit-scrollbar-thumb {
  background: #e8d9c9;
  border-radius: 9999px;
}
</style>
