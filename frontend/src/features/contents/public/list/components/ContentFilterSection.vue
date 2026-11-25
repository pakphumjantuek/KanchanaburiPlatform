<script setup lang="ts">
import type { ContentCategory, District, SubDistrict, Tag } from '../../api/contentApi'
import AppSelect from '@/components/common/input/AppSelect.vue'

interface Props {
  categories: ContentCategory[]
  districts: District[]
  subDistricts: SubDistrict[]
  tags: Tag[]
  sortOptions: { id: string; title: string }[]
  activeFilterCount: number
  categoryId: string | null
  districtId: string | null
  subDistrictId: string | null
  tagId: string | null
  sortBy: 'latest' | 'popular' | 'title'
  filtersOpen: boolean
}

defineProps<Props>()

const emit = defineEmits<{
  'update:categoryId': [val: string | null]
  'update:districtId': [val: string | null]
  'update:subDistrictId': [val: string | null]
  'update:tagId': [val: string | null]
  'update:sortBy': [val: 'latest' | 'popular' | 'title']
  'update:filtersOpen': [val: boolean]
  'change-district': []
  'select-tag': [tagId: string]
  'clear-filters': []
  'filter-change': []
}>()
</script>

<template>
  <section class="relative z-30 overflow-hidden rounded-[1.5rem] border-2 border-[#E8D9C9] bg-white text-[#332820] shadow-sm">
    <!-- Header Toggle Button -->
    <button
      type="button"
      class="flex w-full cursor-pointer select-none items-center justify-between bg-white px-5 py-4 text-left outline-none transition hover:bg-[#FFF9F2] focus:outline-none"
      :aria-expanded="filtersOpen"
      @click="emit('update:filtersOpen', !filtersOpen)"
    >
      <div class="flex items-center gap-3">
        <span class="flex h-9 w-9 items-center justify-center rounded-xl bg-[#D96C2C] text-white shadow-sm">
          <i class="mdi mdi-tune-variant text-base" />
        </span>
        <span class="text-sm font-black whitespace-nowrap text-[#332820]">ตัวกรองค้นหาละเอียด</span>
        <span
          v-if="activeFilterCount"
          class="rounded-full bg-orange-100 text-orange-950 border border-orange-300 px-2.5 py-0.5 text-xs font-black shadow-2xs"
        >
          {{ activeFilterCount }} ตัวกรองที่เลือก
        </span>
      </div>
      <div class="flex items-center gap-2 rounded-full border border-[#E8D9C9] bg-[#F7F0E6] px-3 py-1 text-xs font-bold text-[#786B62]">
        <span class="hidden sm:inline">{{ filtersOpen ? 'ย่อตัวกรอง' : 'ขยายตัวกรอง' }}</span>
        <i
          class="mdi text-lg text-[#786B62] transition-transform duration-200"
          :class="filtersOpen ? 'mdi-chevron-up' : 'mdi-chevron-down'"
        />
      </div>
    </button>

    <!-- Expanded Filter Fields -->
    <div v-show="filtersOpen" class="space-y-5 border-t-2 border-[#E8D9C9] bg-[#FFF9F2]/70 px-5 py-5">
      <!-- Dropdowns Row -->
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-3.5 items-end">
        <div>
          <label class="block text-xs font-bold text-[#332820] mb-1.5">อำเภอ</label>
          <AppSelect
            :model-value="districtId"
            @update:model-value="
              (val: any) => {
                emit('update:districtId', (val as string | null) ?? null)
                emit('change-district')
              }
            "
            :items="districts"
            item-title="districtName"
            item-value="districtId"
            placeholder="ทุกอำเภอ"
            clearable
          />
        </div>

        <div>
          <label class="block text-xs font-bold text-[#332820] mb-1.5">ตำบล</label>
          <AppSelect
            :model-value="subDistrictId"
            @update:model-value="
              (val: any) => {
                emit('update:subDistrictId', (val as string | null) ?? null)
                emit('filter-change')
              }
            "
            :items="subDistricts"
            item-title="subDistrictName"
            item-value="subDistrictId"
            placeholder="ทุกตำบล"
            :disabled="!districtId"
            clearable
          />
        </div>

        <div>
          <label class="block text-xs font-bold text-[#332820] mb-1.5">แท็กคำค้น</label>
          <AppSelect
            :model-value="tagId"
            @update:model-value="
              (val: any) => {
                emit('update:tagId', (val as string | null) ?? null)
                emit('filter-change')
              }
            "
            :items="tags"
            item-title="tagName"
            item-value="tagId"
            placeholder="เลือกแท็ก"
            clearable
          />
        </div>

        <div>
          <label class="block text-xs font-bold text-[#332820] mb-1.5">เรียงลำดับตาม</label>
          <AppSelect
            :model-value="sortBy"
            @update:model-value="
              (val: any) =>
                emit('update:sortBy', (val as 'latest' | 'popular' | 'title') ?? 'latest')
            "
            :items="sortOptions"
            item-title="title"
            item-value="id"
            placeholder="ล่าสุด"
          />
        </div>
      </div>

      <!-- Tag Pills Filter (Capsules) -->
      <div
        v-if="tags.length"
        class="flex flex-wrap items-center justify-between gap-3 border-t border-[#E8D9C9] pt-4"
      >
        <div class="flex flex-wrap items-center gap-1.5">
          <span class="text-xs font-bold text-[#786B62] mr-1">แท็กยอดนิยม:</span>
          <button
            v-for="tg in tags"
            :key="tg.tagId"
            type="button"
            class="px-3 py-1 rounded-full border text-xs font-bold transition-all cursor-pointer active:scale-95 shadow-2xs select-none outline-none focus:outline-none"
            :class="
              tagId === tg.tagId
                ? 'bg-[#D96C2C] text-white border-[#D96C2C] shadow-xs'
                : 'bg-white border-[#E8D9C9] text-[#332820] hover:border-[#D96C2C]'
            "
            @click="emit('select-tag', tg.tagId)"
          >
            #{{ tg.tagName }}
          </button>
        </div>

        <button
          v-if="activeFilterCount"
          type="button"
          class="rounded-full px-3.5 py-1 text-xs font-bold bg-[#F7F0E6] hover:bg-[#E8D9C9] text-[#332820] border border-[#E8D9C9] flex items-center gap-1 transition cursor-pointer select-none"
          @click="emit('clear-filters')"
        >
          <i class="mdi mdi-refresh text-xs"></i>
          <span>ล้างตัวกรอง ({{ activeFilterCount }})</span>
        </button>
      </div>
    </div>
  </section>
</template>
