<script setup lang="ts">
import { youtubeThumbnail } from '@/shared/utils/youtube'
import type { PublicContent } from '../../api/contentApi'

interface Props {
  items: PublicContent[]
}

defineProps<Props>()

// maxresdefault does not exist for every video; fall back to hqdefault
function fallbackThumb(event: Event, youtubeUrl?: string | null) {
  const img = event.target as HTMLImageElement
  const fallback = youtubeThumbnail(youtubeUrl)
  if (fallback && img.src !== fallback) img.src = fallback
}

</script>

<template>
  <section v-if="items.length" class="rounded-3xl bg-white border border-[#E8D9C9] p-6 sm:p-8 shadow-sm space-y-6">
    <div class="flex flex-wrap items-center justify-between gap-3 border-b border-[#E8D9C9] pb-4">
      <div class="flex items-center gap-3">
        <div class="h-10 w-10 rounded-xl bg-[#FFF1E8] text-[#D96C2C] flex items-center justify-center font-bold shadow-xs">
          <i class="mdi mdi-book-open-page-variant-outline text-xl"></i>
        </div>
        <div>
          <h2 class="text-xl sm:text-2xl font-black text-[#332820]">คอนเทนต์เรื่องราวที่เกี่ยวข้อง</h2>
          <p class="text-xs sm:text-sm text-[#786B62] font-semibold">สำรวจเรื่องราวแนะนำจากสถานที่ใกล้เคียงในกาญจนบุรี</p>
        </div>
      </div>
      <RouterLink
        to="/contents"
        class="inline-flex items-center gap-1.5 px-4 py-2 rounded-full bg-[#FFF9F2] border border-[#E8D9C9] text-xs sm:text-sm font-bold text-[#D96C2C] hover:bg-[#D96C2C] hover:text-white hover:border-[#D96C2C] transition shadow-xs"
      >
        <span>ดูคอนเทนต์ทั้งหมด</span>
        <i class="mdi mdi-arrow-right"></i>
      </RouterLink>
    </div>

    <!-- Content Cards Grid -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
      <RouterLink
        v-for="item in items.slice(0, 4)"
        :key="item.contentId"
        :to="`/contents/${item.contentId}`"
        class="group flex flex-col justify-between rounded-2xl bg-[#FFF9F2] border border-[#E8D9C9] overflow-hidden shadow-2xs hover:shadow-md hover:border-[#D96C2C] transition duration-300 transform hover:-translate-y-1"
      >
        <!-- 16:9 Thumbnail -->
        <div class="relative aspect-[16/9] w-full overflow-hidden bg-[#171412]">
          <img
            v-if="youtubeThumbnail(item.youtubeUrl)"
            :src="youtubeThumbnail(item.youtubeUrl, 'maxresdefault')"
            @error="fallbackThumb($event, item.youtubeUrl)"
            :alt="item.title"
            class="h-full w-full object-cover group-hover:scale-105 transition duration-500"
          />
          <div
            v-else
            class="h-full w-full bg-gradient-to-br from-[#D96C2C] to-[#171412] flex items-center justify-center text-white"
          >
            <i class="mdi mdi-compass-rose text-3xl"></i>
          </div>

          <div class="absolute inset-0 bg-slate-950/10 group-hover:bg-slate-950/20 transition"></div>

          <!-- Category Capsule Badge -->
          <span class="absolute top-2.5 left-2.5 px-3 py-1 rounded-full bg-[#D96C2C] text-white text-xs font-bold shadow-xs">
            {{ item.contentCategoryName || 'วัฒนธรรม' }}
          </span>
        </div>

        <!-- Content Info -->
        <div class="p-4 space-y-3 flex-1 flex flex-col justify-between">
          <div>
            <h4 class="font-bold text-[#332820] text-sm sm:text-base line-clamp-2 group-hover:text-[#D96C2C] transition leading-snug">
              {{ item.title }}
            </h4>
          </div>

          <div class="pt-2.5 border-t border-[#E8D9C9] flex items-center justify-between text-xs font-semibold text-[#786B62]">
            <span class="flex items-center gap-1 text-[#D96C2C] font-bold">
              <i class="mdi mdi-map-marker text-[#D96C2C]"></i>
              อ.{{ item.districtName || 'กาญจนบุรี' }}
            </span>
            <span class="flex items-center gap-1">
              <i class="mdi mdi-play-circle-outline text-[#786B62]"></i>
              ดูวิดีโอ
            </span>
          </div>
        </div>
      </RouterLink>
    </div>
  </section>
</template>
