<script setup lang="ts">
import { formatDate } from '@/shared/utils/formatDate'
import { youtubeThumbnail } from '@/shared/utils/youtube'
import type { FavoriteContent } from '../api/profileApi'

interface Props {
  favorites: FavoriteContent[]
}

defineProps<Props>()




</script>

<template>
  <div class="rounded-3xl border border-[#E8D9C9] bg-white p-6 sm:p-8 shadow-xs space-y-6">
    <!-- Header -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-[#E8D9C9] pb-4">
      <div>
        <div class="flex items-center gap-2">
          <span class="flex h-8 w-8 items-center justify-center rounded-xl bg-rose-100 text-rose-600">
            <i class="mdi mdi-heart text-lg"></i>
          </span>
          <h2 class="text-xl sm:text-2xl font-black text-[#332820]">
            รายการโปรดที่บันทึกไว้
          </h2>
          <span
            v-if="favorites.length > 0"
            class="rounded-full bg-rose-50 text-rose-700 border border-rose-200 text-xs font-black px-2.5 py-0.5 ml-1"
          >
            {{ favorites.length }} เรื่อง
          </span>
        </div>
        <p class="text-xs sm:text-sm text-[#786B62] font-semibold mt-1">
          รวมเรื่องราว สถานที่ท่องเที่ยว ร้านอาหาร และกิจกรรมที่คุณชื่นชอบและกดบันทึกเก็บไว้
        </p>
      </div>

      <RouterLink
        to="/contents"
        class="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl border border-[#E8D9C9] bg-white text-xs font-black text-[#786B62] hover:text-[#D96C2C] hover:border-[#D96C2C] transition shadow-2xs self-start sm:self-auto"
      >
        <i class="mdi mdi-compass-outline text-sm text-[#D96C2C]"></i>
        <span>สำรวจคอนเทนต์เพิ่ม</span>
      </RouterLink>
    </div>

    <!-- Empty State -->
    <div
      v-if="!favorites.length"
      class="text-center py-16 px-4 rounded-2xl bg-[#F7F0E6] border-2 border-dashed border-[#E8D9C9] space-y-3"
    >
      <div class="mx-auto flex h-16 w-16 items-center justify-center rounded-2xl bg-rose-100 text-rose-600 shadow-xs">
        <i class="mdi mdi-heart-outline text-3xl"></i>
      </div>
      <h3 class="text-base font-black text-[#332820]">ยังไม่มีรายการโปรด</h3>
      <p class="text-xs text-[#786B62] max-w-sm mx-auto font-semibold">
        คุณสามารถกดบันทึกบทความหรือสถานที่ท่องเที่ยวที่ถูกใจ เพื่อกลับมาอ่านในภายหลังได้
      </p>
      <div class="pt-2">
        <RouterLink
          to="/contents"
          class="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-[#D96C2C] to-[#BF5720] hover:from-[#BF5720] hover:to-[#a84614] text-white font-black text-xs shadow-md shadow-[#D96C2C]/25 transition"
        >
          <i class="mdi mdi-compass text-sm"></i>
          <span>สำรวจเรื่องราวและสถานที่</span>
        </RouterLink>
      </div>
    </div>

    <!-- Favorites Grid -->
    <div v-else class="grid grid-cols-1 gap-4 sm:gap-5 md:grid-cols-2 2xl:grid-cols-3">
      <div
        v-for="item in favorites"
        :key="item.contentId"
        class="group flex flex-col justify-between overflow-hidden rounded-2xl border border-[#E8D9C9] bg-white transition-all duration-300 hover:border-[#D96C2C] hover:shadow-md"
      >
        <div class="relative h-44 w-full bg-[#171412] overflow-hidden">
          <img
            v-if="youtubeThumbnail(item.youtubeUrl)"
            :src="youtubeThumbnail(item.youtubeUrl)"
            :alt="item.title"
            class="h-full w-full object-cover group-hover:scale-105 transition-transform duration-500 ease-out"
          />
          <div
            v-else
            class="flex h-full w-full items-center justify-center bg-gradient-to-br from-[#D96C2C]/30 to-[#171412] text-white"
          >
            <i class="mdi mdi-heart text-4xl text-rose-500/60"></i>
          </div>

          <div class="absolute inset-0 bg-gradient-to-t from-black/60 via-transparent to-transparent pointer-events-none"></div>

          <!-- Heart floating badge -->
          <span
            class="absolute top-3 right-3 h-8 w-8 rounded-full bg-white/90 backdrop-blur-xs flex items-center justify-center text-rose-600 shadow-md ring-2 ring-white/50"
            title="รายการโปรด"
          >
            <i class="mdi mdi-heart text-base"></i>
          </span>
        </div>

        <div class="p-4 sm:p-5 space-y-3 flex-1 flex flex-col justify-between">
          <div>
            <h4 class="font-black text-[#332820] text-sm sm:text-base line-clamp-2 group-hover:text-[#D96C2C] transition-colors leading-snug">
              {{ item.title }}
            </h4>
            <p v-if="item.summary" class="text-xs text-[#786B62] font-semibold line-clamp-2 mt-1.5 leading-relaxed">
              {{ item.summary }}
            </p>
          </div>

          <div class="pt-3 border-t border-[#E8D9C9] flex items-center justify-between text-xs">
            <span class="text-[11px] text-[#786B62] font-semibold flex items-center gap-1">
              <i class="mdi mdi-clock-outline text-[#D96C2C]"></i>
              {{ formatDate(item.createdAt, { dateStyle: 'medium' }) }}
            </span>

            <RouterLink
              :to="`/contents/${item.contentId}`"
              class="inline-flex items-center gap-1 px-3.5 py-1.5 rounded-xl bg-[#F7F0E6] group-hover:bg-[#D96C2C] group-hover:text-white text-[#332820] font-black text-xs transition border border-[#E8D9C9]"
            >
              <span>อ่านต่อ</span>
              <i class="mdi mdi-arrow-right text-xs"></i>
            </RouterLink>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
