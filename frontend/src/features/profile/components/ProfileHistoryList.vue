<script setup lang="ts">
import { formatDate } from '@/shared/utils/formatDate'
import { youtubeThumbnail } from '@/shared/utils/youtube'
import type { ContentViewHistory } from '../api/profileApi'

interface Props {
  history: ContentViewHistory[]
}

defineProps<Props>()




</script>

<template>
  <div class="rounded-3xl border border-[#E8D9C9] bg-white p-6 sm:p-8 shadow-xs space-y-6">
    <!-- Header -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-[#E8D9C9] pb-4">
      <div>
        <div class="flex items-center gap-2">
          <span class="flex h-8 w-8 items-center justify-center rounded-xl bg-amber-100 text-amber-800">
            <i class="mdi mdi-clock-check-outline text-lg"></i>
          </span>
          <h2 class="text-xl sm:text-2xl font-black text-[#332820]">
            ประวัติการเข้าชม
          </h2>
          <span
            v-if="history.length > 0"
            class="rounded-full bg-amber-50 text-amber-800 border border-amber-200 text-xs font-black px-2.5 py-0.5 ml-1"
          >
            {{ history.length }} รายการ
          </span>
        </div>
        <p class="text-xs sm:text-sm text-[#786B62] font-semibold mt-1">
          เรื่องราว บทความท่องเที่ยว และสถานที่ในเมืองกาญจน์ที่คุณเคยเปิดดูย้อนหลัง
        </p>
      </div>

      <RouterLink
        to="/contents"
        class="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl border border-[#E8D9C9] bg-white text-xs font-black text-[#786B62] hover:text-[#D96C2C] hover:border-[#D96C2C] transition shadow-2xs self-start sm:self-auto"
      >
        <i class="mdi mdi-compass-outline text-sm text-[#D96C2C]"></i>
        <span>สำรวจคอนเทนต์</span>
      </RouterLink>
    </div>

    <!-- Empty State -->
    <div
      v-if="!history.length"
      class="text-center py-16 px-4 rounded-2xl bg-[#F7F0E6] border-2 border-dashed border-[#E8D9C9] space-y-3"
    >
      <div class="mx-auto flex h-16 w-16 items-center justify-center rounded-2xl bg-[#D96C2C]/15 text-[#D96C2C]">
        <i class="mdi mdi-history text-3xl"></i>
      </div>
      <h3 class="text-base font-black text-[#332820]">ยังไม่มีประวัติการเข้าชมคอนเทนต์</h3>
      <p class="text-xs text-[#786B62] max-w-sm mx-auto font-semibold">
        เมื่อคุณคลิกชมเรื่องราว สถานที่ท่องเที่ยว หรือร้านค้าต่างๆ รายการจะถูกบันทึกไว้ที่นี่
      </p>
      <div class="pt-2">
        <RouterLink
          to="/contents"
          class="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-[#D96C2C] to-[#BF5720] hover:from-[#BF5720] hover:to-[#a84614] text-white font-black text-xs shadow-md shadow-[#D96C2C]/25 transition"
        >
          <i class="mdi mdi-compass text-sm"></i>
          <span>สำรวจเรื่องราวเมืองกาญจน์</span>
        </RouterLink>
      </div>
    </div>

    <!-- History List Items -->
    <div v-else class="space-y-3">
      <div
        v-for="item in history"
        :key="item.contentId"
        class="group flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 p-4 rounded-2xl border border-[#E8D9C9] bg-white transition-all duration-200 hover:border-[#D96C2C] hover:shadow-xs"
      >
        <div class="flex items-center gap-4 min-w-0 flex-1">
          <!-- Small Thumbnail -->
          <div class="relative h-16 w-20 sm:h-18 sm:w-24 rounded-xl bg-[#171412] shrink-0 overflow-hidden border border-[#E8D9C9]">
            <img
              v-if="youtubeThumbnail(item.youtubeUrl)"
              :src="youtubeThumbnail(item.youtubeUrl)"
              :alt="item.title"
              class="h-full w-full object-cover group-hover:scale-105 transition-transform duration-300"
            />
            <div
              v-else
              class="flex h-full w-full items-center justify-center text-slate-400 bg-gradient-to-br from-[#D96C2C]/20 to-[#171412]"
            >
              <i class="mdi mdi-play-circle-outline text-2xl text-[#D96C2C]"></i>
            </div>
            <span class="absolute bottom-1 right-1 h-5 w-5 rounded bg-black/70 flex items-center justify-center text-white text-[10px]">
              <i class="mdi mdi-eye"></i>
            </span>
          </div>

          <!-- Info -->
          <div class="min-w-0 flex-1 space-y-1">
            <h4 class="font-black text-[#332820] text-sm sm:text-base truncate group-hover:text-[#D96C2C] transition-colors leading-snug">
              {{ item.title }}
            </h4>
            <p v-if="item.summary" class="text-xs text-[#786B62] font-semibold line-clamp-1">
              {{ item.summary }}
            </p>
            <span class="text-[11px] text-[#786B62] font-semibold flex items-center gap-1">
              <i class="mdi mdi-clock-outline text-[#D96C2C]"></i>
              เข้าชมเมื่อ: {{ formatDate(item.viewedAt, { dateStyle: 'medium', timeStyle: 'short' }) }}
            </span>
          </div>
        </div>

        <!-- Action Button -->
        <RouterLink
          :to="`/contents/${item.contentId}`"
          class="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-[#F7F0E6] group-hover:bg-[#D96C2C] group-hover:text-white text-xs font-black text-[#332820] transition border border-[#E8D9C9] shrink-0 self-end sm:self-auto"
        >
          <span>เปิดดูอีกครั้ง</span>
          <i class="mdi mdi-arrow-right text-xs transition-transform group-hover:translate-x-1"></i>
        </RouterLink>
      </div>
    </div>
  </div>
</template>
