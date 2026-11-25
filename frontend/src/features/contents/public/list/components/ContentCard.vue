<script setup lang="ts">
import { formatDate } from '@/shared/utils/formatDate'
import { youtubeThumbnail } from '@/shared/utils/youtube'
import type { PublicContent } from '../../api/contentApi'

interface Props {
  item: PublicContent
  displayMode?: 'grid' | 'list'
}

withDefaults(defineProps<Props>(), {
  displayMode: 'grid',
})

// maxresdefault does not exist for every video; fall back to the always-available hqdefault
function fallbackThumb(event: Event, youtubeUrl?: string | null) {
  const img = event.target as HTMLImageElement
  const fallback = youtubeThumbnail(youtubeUrl)
  if (fallback && img.src !== fallback) img.src = fallback
}
</script>

<template>
  <!-- GRID MODE CARD -->
  <RouterLink
    v-if="displayMode === 'grid'"
    :to="`/contents/${item.contentId}`"
    class="group flex flex-col justify-between overflow-hidden rounded-3xl border border-[#E8D9C9] bg-white transition-all duration-300 ease-out hover:-translate-y-1 hover:border-[#D96C2C]/60 hover:shadow-xl hover:shadow-[#332820]/10 focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25 focus-visible:outline-none"
  >
    <div>
      <!-- Thumbnail -->
      <div class="relative aspect-16/10 w-full overflow-hidden bg-slate-900">
        <img
          v-if="youtubeThumbnail(item.youtubeUrl)"
          :src="youtubeThumbnail(item.youtubeUrl, 'maxresdefault')"
          @error="fallbackThumb($event, item.youtubeUrl)"
          :alt="item.title"
          loading="lazy"
          class="h-full w-full object-cover transition-transform duration-700 ease-out group-hover:scale-105"
        />
        <div
          v-else
          class="flex h-full w-full items-center justify-center bg-gradient-to-br from-amber-800 via-orange-900 to-slate-900 text-white"
        >
          <i
            class="mdi mdi-compass-rose text-5xl opacity-40 transition-transform duration-500 group-hover:scale-110 group-hover:rotate-12"
          ></i>
        </div>

        <div class="absolute inset-0 bg-gradient-to-t from-slate-950/70 via-slate-950/0 to-slate-950/20"></div>

        <!-- Category chip -->
        <span
          v-if="item.contentCategoryName"
          class="absolute top-3 left-3 flex max-w-[60%] items-center gap-1.5 rounded-full bg-white/95 px-3 py-1 text-xs font-black text-[#BF5720] shadow-sm"
        >
          <i class="mdi mdi-tag-outline text-xs"></i>
          <span class="truncate">{{ item.contentCategoryName }}</span>
        </span>

        <!-- District chip -->
        <span
          v-if="item.districtName"
          class="absolute top-3 right-3 flex items-center gap-1 rounded-full bg-black/55 px-3 py-1 text-xs font-bold text-white backdrop-blur-md"
        >
          <i class="mdi mdi-map-marker text-xs text-[#F2A65A]"></i>
          <span>อ.{{ item.districtName }}</span>
        </span>

        <!-- Video play indicator -->
        <div
          v-if="item.youtubeUrl"
          class="absolute bottom-3 right-3 flex h-10 w-10 items-center justify-center rounded-full bg-[#D96C2C] text-white shadow-lg transition-transform duration-300 group-hover:scale-110"
        >
          <i class="mdi mdi-play ml-0.5 text-xl text-white"></i>
        </div>
      </div>

      <!-- Body -->
      <div class="space-y-2 p-5">
        <h3
          class="line-clamp-2 text-base leading-snug font-black text-[#332820] transition-colors group-hover:text-[#D96C2C] sm:text-lg"
        >
          {{ item.title }}
        </h3>
        <p class="line-clamp-2 text-xs leading-relaxed font-medium text-[#786B62] sm:text-sm">
          {{ item.summary || 'สำรวจเรื่องราวการท่องเที่ยว สถานที่ วัฒนธรรม และสินค้าชุมชนกาญจนบุรี' }}
        </p>
      </div>
    </div>

    <!-- Footer -->
    <div
      class="flex flex-wrap items-center justify-between gap-x-3 gap-y-2 border-t border-dashed border-[#E8D9C9] px-5 py-3.5 text-xs font-bold text-[#786B62]"
    >
      <div class="flex items-center gap-3">
        <span class="flex items-center gap-1.5 text-[#332820]">
          <i class="mdi mdi-calendar-blank-outline text-sm text-[#D96C2C]"></i>
          {{ formatDate(item.publishedAt ?? item.createdAt, { day: 'numeric', month: 'short', year: 'numeric' }, 'เรื่องราวกาญจนบุรี') }}
        </span>
        <span class="flex items-center gap-1 text-[#332820]">
          <i class="mdi mdi-eye-outline text-sm text-[#D96C2C]"></i>
          {{ (item.viewCount || 0).toLocaleString('th-TH') }}
        </span>
      </div>

      <div
        class="flex items-center gap-1 rounded-full bg-[#FFF1E8] px-3 py-1.5 text-[#BF5720] transition group-hover:bg-[#D96C2C] group-hover:text-white"
      >
        <span>อ่านต่อ</span>
        <i class="mdi mdi-arrow-right text-sm transition-transform duration-200 group-hover:translate-x-0.5"></i>
      </div>
    </div>
  </RouterLink>

  <!-- LIST MODE CARD -->
  <RouterLink
    v-else
    :to="`/contents/${item.contentId}`"
    class="group flex flex-col overflow-hidden rounded-3xl border border-[#E8D9C9] bg-white transition-all duration-300 ease-out hover:-translate-y-1 hover:border-[#D96C2C]/60 hover:shadow-xl hover:shadow-[#332820]/10 focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25 focus-visible:outline-none sm:flex-row"
  >
    <div class="relative aspect-16/10 w-full shrink-0 overflow-hidden bg-slate-900 sm:aspect-auto sm:w-72">
      <img
        v-if="youtubeThumbnail(item.youtubeUrl)"
        :src="youtubeThumbnail(item.youtubeUrl, 'maxresdefault')"
          @error="fallbackThumb($event, item.youtubeUrl)"
        :alt="item.title"
        loading="lazy"
        class="h-full w-full object-cover transition-transform duration-700 ease-out group-hover:scale-105"
      />
      <div
        v-else
        class="flex h-full w-full items-center justify-center bg-gradient-to-br from-amber-800 via-orange-900 to-slate-900 text-white"
      >
        <i class="mdi mdi-compass-rose text-5xl opacity-40"></i>
      </div>

      <!-- Play button badge -->
      <div
        v-if="item.youtubeUrl"
        class="absolute bottom-3 right-3 flex h-10 w-10 items-center justify-center rounded-full bg-[#D96C2C] text-white shadow-lg transition-transform duration-300 group-hover:scale-110"
      >
        <i class="mdi mdi-play ml-0.5 text-xl text-white"></i>
      </div>
    </div>

    <div class="flex flex-1 flex-col justify-between space-y-3 p-5 sm:p-6">
      <div>
        <div class="mb-2 flex flex-wrap items-center gap-2">
          <span
            v-if="item.contentCategoryName"
            class="rounded-full border border-orange-200 bg-orange-50 px-3 py-0.5 text-xs font-bold text-orange-900"
          >
            {{ item.contentCategoryName }}
          </span>
          <span
            v-if="item.districtName"
            class="flex items-center gap-1 rounded-full border border-[#E8D9C9] bg-[#F7F0E6] px-2.5 py-0.5 text-xs font-bold text-[#786B62]"
          >
            <i class="mdi mdi-map-marker text-[#D96C2C]"></i>
            อ.{{ item.districtName }}
          </span>
        </div>
        <h3
          class="text-lg leading-snug font-black text-[#332820] transition-colors group-hover:text-[#D96C2C] sm:text-xl"
        >
          {{ item.title }}
        </h3>
        <p class="mt-1.5 line-clamp-2 text-xs leading-relaxed font-medium text-[#786B62] sm:text-sm">
          {{ item.summary || 'สำรวจเรื่องราวการท่องเที่ยว สถานที่ วัฒนธรรม และสินค้าชุมชนกาญจนบุรี' }}
        </p>
      </div>

      <div
        class="flex items-center justify-between border-t border-[#E8D9C9] pt-3 text-xs font-bold text-[#786B62]"
      >
        <div class="flex items-center gap-3">
          <span class="flex items-center gap-1.5 text-[#332820]">
            <i class="mdi mdi-calendar-blank-outline text-[#D96C2C]"></i>
            {{ formatDate(item.publishedAt ?? item.createdAt, { day: 'numeric', month: 'short', year: 'numeric' }, 'เรื่องราวกาญจนบุรี') }}
          </span>
          <span class="flex items-center gap-1 text-[#332820]">
            <i class="mdi mdi-eye-outline text-[#D96C2C]"></i>
            {{ (item.viewCount || 0).toLocaleString('th-TH') }} ครั้ง
          </span>
        </div>
        <div
          class="flex items-center gap-1 rounded-full bg-[#FFF1E8] px-3 py-1.5 text-[#BF5720] transition group-hover:bg-[#D96C2C] group-hover:text-white"
        >
          <span>อ่านต่อ</span>
          <i class="mdi mdi-arrow-right text-sm transition-transform duration-200 group-hover:translate-x-0.5"></i>
        </div>
      </div>
    </div>
  </RouterLink>
</template>
