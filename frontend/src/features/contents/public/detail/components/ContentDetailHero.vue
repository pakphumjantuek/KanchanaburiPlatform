<script setup lang="ts">
import { ref, watch } from 'vue'
import { formatDate } from '@/shared/utils/formatDate'
import { youtubeThumbnail } from '@/shared/utils/youtube'
import type { PublicContent } from '../../api/contentApi'

interface Props {
  content: PublicContent
  isFavorite: boolean
  changingFavorite?: boolean
}

const props = defineProps<Props>()

// The video only loads after the visitor presses play (no autoplay on page open)
const playing = ref(false)
watch(
  () => props.content.contentId,
  () => {
    playing.value = false
  },
)

// maxresdefault does not exist for every video; fall back to hqdefault
function fallbackPoster(event: Event) {
  const img = event.target as HTMLImageElement
  const fallback = youtubeThumbnail(props.content.youtubeUrl)
  if (fallback && img.src !== fallback) img.src = fallback
}

const emit = defineEmits<{
  'toggle-favorite': []
  share: [platform?: string]
}>()

function youtubeEmbedUrl(url?: string) {
  if (!url) return ''
  try {
    const parsed = new URL(url)
    const videoId = parsed.hostname.includes('youtu.be')
      ? parsed.pathname.slice(1)
      : (parsed.searchParams.get('v') ??
          (parsed.pathname.startsWith('/embed/') ? parsed.pathname.split('/')[2] : ''))
    return videoId
      ? `https://www.youtube-nocookie.com/embed/${videoId}?autoplay=1&rel=0`
      : ''
  } catch {
    return ''
  }
}


</script>

<template>
  <div class="space-y-5">
    <!-- 16:9 Cinema Video Player Container -->
    <div
      class="relative overflow-hidden rounded-3xl border border-[#E8D9C9] bg-[#171412] shadow-xl group aspect-[16/9] w-full"
    >
      <iframe
        v-if="playing && youtubeEmbedUrl(content.youtubeUrl)"
        :src="youtubeEmbedUrl(content.youtubeUrl)"
        :title="content.title"
        class="h-full w-full border-0 relative z-10"
        allow="autoplay; encrypted-media; picture-in-picture; fullscreen"
        allowfullscreen
      />
      <button
        v-else-if="youtubeEmbedUrl(content.youtubeUrl)"
        type="button"
        class="relative z-10 block h-full w-full cursor-pointer overflow-hidden outline-none focus-visible:ring-4 focus-visible:ring-[#D96C2C]/50"
        :aria-label="`เล่นวิดีโอ ${content.title}`"
        @click="playing = true"
      >
        <img
          v-if="youtubeThumbnail(content.youtubeUrl)"
          :src="youtubeThumbnail(content.youtubeUrl, 'maxresdefault')"
          :alt="content.title"
          class="h-full w-full object-cover transition-transform duration-700 ease-out group-hover:scale-105"
          @error="fallbackPoster"
        />
        <div class="absolute inset-0 bg-gradient-to-t from-black/60 via-black/10 to-black/20"></div>
        <span
          class="absolute top-1/2 left-1/2 flex h-20 w-20 -translate-x-1/2 -translate-y-1/2 items-center justify-center rounded-full bg-[#D96C2C] text-white shadow-xl shadow-[#D96C2C]/40 ring-4 ring-[#F2A65A]/40 transition duration-300 group-hover:scale-110"
        >
          <i class="mdi mdi-play ml-1 text-5xl"></i>
        </span>
      </button>
      <a
        v-else
        :href="content.youtubeUrl || '#'"
        target="_blank"
        rel="noopener"
        class="relative z-10 flex h-full w-full flex-col items-center justify-center gap-4 bg-gradient-to-br from-[#D96C2C] via-[#BF5720] to-[#171412] text-center text-white transition hover:brightness-105"
      >
        <div
          class="h-20 w-20 rounded-full bg-[#D96C2C] text-white flex items-center justify-center shadow-xl shadow-[#D96C2C]/40 transform group-hover:scale-110 transition duration-300 ring-4 ring-[#F2A65A]/40"
        >
          <i class="mdi mdi-play text-5xl ml-1 text-white"></i>
        </div>
        <div>
          <span class="text-lg font-black text-white block">รับชมวิดีโอแนะนำสถานที่บน YouTube</span>
          <span class="text-xs text-[#F2A65A] font-extrabold mt-1 block"
            >คลิกเพื่อเปิดเล่นวิดีโอคุณภาพสูง</span
          >
        </div>
      </a>

      <!-- Video Overlay Badge -->
      <div class="absolute top-4 left-4 z-20 flex items-center gap-2">
        <span
          class="px-3.5 py-1.5 rounded-full text-sm font-semibold bg-[#D96C2C] text-white border border-white/20 backdrop-blur-md shadow-md flex items-center gap-1.5"
        >
          <i class="mdi mdi-compass-rose text-base text-white"></i>
          {{ content.contentCategoryName || 'เรื่องราวกาญจนบุรี' }}
        </span>
      </div>
    </div>

    <!-- Video Title & Main Action Bar -->
    <div class="rounded-3xl bg-white border border-[#E8D9C9] p-6 shadow-sm space-y-4">
      <div class="flex flex-wrap items-center justify-between gap-3">
        <!-- Category & Location Badge -->
        <div class="flex flex-wrap items-center gap-2 text-xs sm:text-sm font-bold text-[#D96C2C]">
          <span class="bg-[#FFF9F2] text-[#D96C2C] px-3.5 py-1.5 rounded-full flex items-center gap-1.5 border border-[#E8D9C9] shadow-2xs">
            <i class="mdi mdi-tag-outline"></i>
            {{ content.contentCategoryName || 'เรื่องราวกาญจนบุรี' }}
          </span>
          <span class="bg-[#FFF9F2] text-[#D96C2C] px-3.5 py-1.5 rounded-full flex items-center gap-1.5 border border-[#E8D9C9] shadow-2xs">
            <i class="mdi mdi-map-marker text-[#D96C2C]"></i>
            {{ content.districtName ? `อ.${content.districtName} ` : '' }}จ.กาญจนบุรี
          </span>
        </div>

        <!-- Action Buttons: Bookmark & Share (Capsule Pills) -->
        <div class="flex items-center gap-2">
          <button
            type="button"
            class="flex items-center gap-1.5 px-4 py-2 rounded-full border border-[#E8D9C9] bg-white text-[#332820] font-bold text-xs sm:text-sm hover:border-[#D96C2C] hover:text-[#D96C2C] transition cursor-pointer active:scale-95 shadow-2xs outline-none focus:outline-none select-none"
            @click="emit('share')"
          >
            <i class="mdi mdi-share-variant-outline text-base text-[#D96C2C]"></i>
            <span>แชร์</span>
          </button>

          <button
            type="button"
            class="flex items-center gap-1.5 px-4 py-2 rounded-full border-2 transition font-bold text-xs sm:text-sm active:scale-95 cursor-pointer shadow-2xs outline-none focus:outline-none select-none"
            :class="
              isFavorite
                ? 'border-rose-300 bg-rose-50 text-rose-600'
                : 'border-[#E8D9C9] bg-white text-[#332820] hover:border-[#D96C2C]'
            "
            :disabled="changingFavorite"
            @click="emit('toggle-favorite')"
          >
            <i
              class="mdi text-base"
              :class="isFavorite ? 'mdi-heart text-rose-600' : 'mdi-heart-outline text-[#D96C2C]'"
            ></i>
            <span>{{ isFavorite ? 'บันทึกแล้ว' : 'บันทึก' }}</span>
          </button>
        </div>
      </div>

      <!-- Title -->
      <h1 class="text-2xl sm:text-3xl xl:text-4xl font-black text-[#332820] leading-[1.35] tracking-tight">
        {{ content.title }}
      </h1>

      <!-- Metadata Statistics Row -->
      <div class="grid grid-cols-1 gap-3 text-xs font-semibold text-[#4A3E35] sm:grid-cols-3 sm:text-sm">
        <span class="flex items-center gap-3 rounded-2xl bg-[#FFF9F2] px-4 py-3">
          <span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-[#FFF1E8] text-lg text-[#D96C2C]">
            <i class="mdi mdi-calendar-blank-outline"></i>
          </span>
          <span class="min-w-0">
            <span class="block text-[11px] text-[#786B62]">เผยแพร่เมื่อ</span>
            <span class="block truncate font-black text-[#332820]">{{
              formatDate(content.publishedAt || content.createdAt, { dateStyle: 'long' })
            }}</span>
          </span>
        </span>
        <span class="flex items-center gap-3 rounded-2xl bg-[#FFF9F2] px-4 py-3">
          <span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-[#FFF1E8] text-lg text-[#D96C2C]">
            <i class="mdi mdi-eye-outline"></i>
          </span>
          <span class="min-w-0">
            <span class="block text-[11px] text-[#786B62]">ยอดเข้าชม</span>
            <span class="block font-black text-[#332820]">{{ (content.viewCount || 0).toLocaleString('th-TH') }} ครั้ง</span>
          </span>
        </span>
        <span v-if="content.shopName" class="flex items-center gap-3 rounded-2xl bg-[#FFF9F2] px-4 py-3">
          <span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-[#FFF1E8] text-lg text-[#D96C2C]">
            <i class="mdi mdi-store-outline"></i>
          </span>
          <span class="min-w-0">
            <span class="block text-[11px] text-[#786B62]">ผู้เผยแพร่</span>
            <span class="block truncate font-black text-[#D96C2C]">{{ content.shopName }}</span>
          </span>
        </span>
      </div>

      <!-- Hashtag Pills (Capsule Pills) -->
      <div v-if="content.tags.length" class="flex flex-wrap gap-2 pt-1">
        <span
          v-for="tag in content.tags"
          :key="tag.tagId"
          class="px-3.5 py-1 rounded-full bg-[#FFF9F2] border border-[#E8D9C9] text-xs font-bold text-[#332820] hover:border-[#D96C2C] hover:text-[#D96C2C] transition shadow-2xs"
        >
          #{{ tag.tagName }}
        </span>
      </div>
    </div>
  </div>
</template>
