<script setup lang="ts">
import { formatDate } from '@/shared/utils/formatDate'
import { youtubeThumbnail } from '@/shared/utils/youtube'
import { vReveal } from '@/shared/directives/reveal'
import type { PublicContent } from '@/features/contents/api'
import type { Shop } from '@/features/shops/api'

withDefaults(
  defineProps<{
    shop: Shop
    shopContents: PublicContent[]
    /** how many cards to show (home tab: 3, contents tab: more) */
    limit?: number
  }>(),
  { limit: 3 },
)

// maxresdefault (16:9, no letterbox bars) does not exist for every video; fall back to hqdefault
function fallbackThumb(event: Event, youtubeUrl?: string | null) {
  const img = event.target as HTMLImageElement
  const fallback = youtubeThumbnail(youtubeUrl)
  if (fallback && img.src !== fallback) img.src = fallback
}
</script>

<template>
  <section
    class="space-y-6 rounded-[1.75rem] border-2 border-[#E8D9C9] bg-white p-6 shadow-md shadow-[#D96C2C]/5 sm:p-8"
  >
    <!-- Header -->
    <div class="flex flex-wrap items-center justify-between gap-4 border-b-2 border-[#E8D9C9] pb-5">
      <div class="flex items-center gap-3.5">
        <div
          class="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-[#D96C2C] text-white shadow-md"
        >
          <i class="mdi mdi-book-open-page-variant-outline text-2xl"></i>
        </div>
        <div>
          <h2 class="text-xl font-black tracking-tight text-[#332820] sm:text-2xl">
            เรื่องราวและคอนเทนต์ชุมชน
          </h2>
          <p class="text-xs font-semibold text-[#786B62] sm:text-sm">
            เรื่องราวจากร้านค้าและผู้ใช้ที่กล่าวถึงร้านนี้
          </p>
        </div>
      </div>

      <RouterLink
        :to="`/shops/${shop.shopId}/contents`"
        class="inline-flex items-center gap-1.5 rounded-full border-2 border-[#E8D9C9] bg-[#FFF9F2] px-4 py-2 text-xs font-bold text-[#D96C2C] transition hover:border-[#D96C2C] hover:bg-[#D96C2C] hover:text-white focus-visible:ring-4 focus-visible:ring-[#D96C2C]/20 focus-visible:outline-none active:scale-95 sm:text-sm"
      >
        <span>ดูคอนเทนต์ทั้งหมด ({{ shopContents.length }})</span>
        <i class="mdi mdi-arrow-right text-sm"></i>
      </RouterLink>
    </div>

    <!-- Cards -->
    <div
      v-if="shopContents.length"
      class="grid grid-cols-1 gap-5 sm:grid-cols-2 sm:gap-6 lg:grid-cols-3"
    >
      <RouterLink
        v-for="(item, idx) in shopContents.slice(0, limit)"
        :key="item.contentId"
        v-reveal
        :to="`/contents/${item.contentId}`"
        :style="{ animationDelay: `${(idx % 3) * 90}ms` }"
        class="group flex flex-col justify-between overflow-hidden rounded-[1.75rem] border-2 border-[#E8D9C9] bg-white shadow-xs transition-all duration-300 ease-out hover:-translate-y-1.5 hover:border-[#D96C2C] hover:shadow-2xl hover:shadow-[#D96C2C]/10 focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25 focus-visible:outline-none"
      >
        <div>
          <!-- Thumbnail -->
          <div class="relative aspect-16/10 w-full overflow-hidden bg-slate-900">
            <img
              v-if="youtubeThumbnail(item.youtubeUrl)"
              :src="youtubeThumbnail(item.youtubeUrl, 'maxresdefault')"
              :alt="item.title"
              loading="lazy"
              class="h-full w-full object-cover transition-transform duration-700 ease-out group-hover:scale-105"
              @error="fallbackThumb($event, item.youtubeUrl)"
            />
            <div
              v-else
              class="flex h-full w-full items-center justify-center bg-gradient-to-br from-[#D96C2C] via-[#BF5720] to-[#332820] text-white"
            >
              <i
                class="mdi mdi-compass-rose text-5xl opacity-40 transition-transform duration-500 group-hover:scale-110 group-hover:rotate-12"
              ></i>
            </div>
            <div class="absolute inset-0 bg-gradient-to-t from-slate-950/70 via-slate-950/0 to-slate-950/20"></div>

            <span
              v-if="item.contentCategoryName"
              class="absolute top-3 left-3 flex max-w-[75%] items-center gap-1.5 rounded-full border border-white/20 bg-[#D96C2C]/95 px-3 py-1 text-xs font-black text-white shadow-md backdrop-blur-md"
            >
              <i class="mdi mdi-tag-outline text-xs"></i>
              <span class="truncate">{{ item.contentCategoryName }}</span>
            </span>

            <div
              v-if="item.youtubeUrl"
              class="absolute right-3 bottom-3 flex h-9 w-9 items-center justify-center rounded-full border border-white/20 bg-slate-950/75 text-white shadow-md backdrop-blur-md transition-transform duration-300 group-hover:scale-105"
            >
              <i class="mdi mdi-play ml-0.5 text-lg text-[#F2A65A]"></i>
            </div>
          </div>

          <!-- Body -->
          <div class="space-y-2 p-5">
            <span
              class="inline-flex items-center gap-1 rounded-full border border-[#E8D9C9] bg-[#FFF9F2] px-2.5 py-0.5 text-[11px] font-bold text-[#786B62]"
            >
              <i
                class="mdi text-xs text-[#D96C2C]"
                :class="item.contentType === 'Shop' ? 'mdi-storefront-outline' : 'mdi-account-voice'"
              ></i>
              {{ item.contentType === 'Shop' ? 'เรื่องราวจากร้าน' : 'ผู้ใช้เล่าถึงร้าน' }}
            </span>
            <h3
              class="line-clamp-2 text-base leading-snug font-black text-[#332820] transition-colors group-hover:text-[#D96C2C] sm:text-lg"
            >
              {{ item.title }}
            </h3>
            <p
              v-if="item.summary"
              class="line-clamp-2 text-xs leading-relaxed font-medium text-[#786B62] sm:text-sm"
            >
              {{ item.summary }}
            </p>
          </div>
        </div>

        <!-- Footer -->
        <div
          class="flex flex-wrap items-center justify-between gap-x-3 gap-y-1.5 border-t border-[#E8D9C9] bg-[#FFF9F2] px-5 py-3.5 text-xs font-bold text-[#786B62]"
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
          <span class="flex items-center gap-1 text-[#D96C2C] transition group-hover:text-[#BF5720]">
            <span>อ่านเรื่องราว</span>
            <i class="mdi mdi-arrow-right text-sm transition-transform duration-200 group-hover:translate-x-1"></i>
          </span>
        </div>
      </RouterLink>
    </div>

    <!-- Empty state -->
    <div
      v-else
      class="space-y-2 rounded-[1.75rem] border-2 border-dashed border-[#E8D9C9] bg-[#FFF9F2] p-8 text-center"
    >
      <i class="mdi mdi-text-box-search-outline text-3xl text-[#786B62]"></i>
      <p class="text-sm font-black text-[#332820]">ยังไม่มีเรื่องราวคอนเทนต์ของร้านค้านี้ในขณะนี้</p>
    </div>
  </section>
</template>
