<script setup lang="ts">
import { formatDate } from '@/shared/utils/formatDate'
import { youtubeThumbnail } from '@/shared/utils/youtube'
import { computed, ref } from 'vue'
import type { UserContent } from '@/features/contents/user/api/userContentApi'

interface Props {
  contents: UserContent[]
}

const props = defineProps<Props>()

const emit = defineEmits<{
  'delete-content': [content: UserContent]
}>()

type ContentTab = 'all' | 'Published' | 'Draft' | 'Pending' | 'Archived'

const currentTab = ref<ContentTab>('all')

const count = (status: UserContent['status']) =>
  props.contents.filter((content) => content.status === status).length

// "ทั้งหมด" = everything that is not archived; "รอตรวจสอบ" only appears when there is something to review
const tabs = computed<{ id: ContentTab; label: string; count: number }[]>(() => {
  const list: { id: ContentTab; label: string; count: number }[] = [
    { id: 'all', label: 'ทั้งหมด', count: props.contents.filter((c) => c.status !== 'Archived').length },
    { id: 'Published', label: 'เผยแพร่แล้ว', count: count('Published') },
    { id: 'Draft', label: 'ฉบับร่าง', count: count('Draft') },
  ]
  if (count('Pending') > 0) list.push({ id: 'Pending', label: 'รอตรวจสอบ', count: count('Pending') })
  list.push({ id: 'Archived', label: 'เก็บถาวร', count: count('Archived') })
  return list
})

const visibleContents = computed(() => {
  if (currentTab.value === 'all') return props.contents.filter((c) => c.status !== 'Archived')
  return props.contents.filter((c) => c.status === currentTab.value)
})

function statusLabel(status: UserContent['status']) {
  if (status === 'Published') return 'เผยแพร่แล้ว'
  if (status === 'Draft') return 'ฉบับร่าง'
  if (status === 'Pending') return 'รอตรวจสอบ'
  return 'เก็บถาวร'
}

function statusDot(status: UserContent['status']) {
  if (status === 'Published') return 'bg-emerald-500'
  if (status === 'Draft') return 'bg-slate-400'
  if (status === 'Pending') return 'bg-amber-500'
  return 'bg-[#C9B8A8]'
}

function statusText(status: UserContent['status']) {
  if (status === 'Published') return 'text-emerald-700'
  if (status === 'Pending') return 'text-amber-700'
  return 'text-[#786B62]'
}

const emptyText = computed(() => {
  if (currentTab.value === 'Archived') return 'ยังไม่มีคอนเทนต์ที่เก็บถาวร'
  if (currentTab.value === 'Draft') return 'ยังไม่มีฉบับร่าง'
  if (currentTab.value === 'Published') return 'ยังไม่มีคอนเทนต์ที่เผยแพร่'
  if (currentTab.value === 'Pending') return 'ไม่มีคอนเทนต์ที่รอตรวจสอบ'
  return 'คุณยังไม่ได้เขียนคอนเทนต์'
})
</script>

<template>
  <div class="space-y-4">
    <!-- Title + main action -->
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h2 class="text-xl leading-[1.35] font-black text-[#332820] sm:text-2xl">คอนเทนต์ของคุณ</h2>
        <p class="text-sm font-semibold text-[#786B62]">เรื่องราวและกิจกรรมที่คุณเขียนบนแพลตฟอร์ม</p>
      </div>
      <RouterLink
        to="/create"
        class="inline-flex shrink-0 items-center gap-2 rounded-full bg-[#D96C2C] px-5 py-2.5 text-sm font-black text-white shadow-md shadow-[#D96C2C]/25 transition hover:bg-[#BF5720] focus-visible:ring-4 focus-visible:ring-[#D96C2C]/30 focus-visible:outline-none active:scale-95"
      >
        <i class="mdi mdi-plus text-lg text-white"></i>
        <span class="text-white">เขียนคอนเทนต์ใหม่</span>
      </RouterLink>
    </div>

    <!-- Status tabs -->
    <div class="rounded-2xl border border-[#E8D9C9] bg-white">
      <div class="flex overflow-x-auto px-2" role="tablist" aria-label="สถานะคอนเทนต์">
        <button
          v-for="t in tabs"
          :key="t.id"
          type="button"
          role="tab"
          :aria-selected="currentTab === t.id"
          class="relative shrink-0 cursor-pointer px-4 py-3.5 text-sm font-black whitespace-nowrap transition focus-visible:outline-none"
          :class="currentTab === t.id ? 'text-[#D96C2C]' : 'text-[#786B62] hover:text-[#332820]'"
          @click="currentTab = t.id"
        >
          {{ t.label }}
          <span class="ml-1 text-xs font-bold" :class="currentTab === t.id ? 'text-[#D96C2C]' : 'text-[#A8988C]'">{{ t.count }}</span>
          <span v-if="currentTab === t.id" class="absolute inset-x-3 bottom-0 h-0.5 rounded-full bg-[#D96C2C]"></span>
        </button>
      </div>
    </div>

    <!-- Empty -->
    <div v-if="!visibleContents.length" class="rounded-2xl border border-[#E8D9C9] bg-white px-6 py-16 text-center">
      <i class="mdi mdi-text-box-plus-outline text-5xl text-[#C9B8A8]"></i>
      <p class="mt-3 text-base font-black text-[#332820]">{{ emptyText }}</p>
      <p v-if="currentTab === 'all'" class="mt-1 text-sm font-semibold text-[#786B62]">
        แบ่งปันสถานที่ท่องเที่ยว ร้านอาหาร หรือเรื่องราวดีๆ ในกาญจนบุรีได้เลย
      </p>
      <RouterLink
        v-if="currentTab === 'all'"
        to="/create"
        class="mt-4 inline-flex items-center gap-2 rounded-full bg-[#D96C2C] px-6 py-2.5 text-sm font-black text-white transition hover:bg-[#BF5720]"
      >
        <span class="text-white">สร้างเรื่องราวแรกของคุณ</span>
      </RouterLink>
    </div>

    <!-- One row per content -->
    <div v-else class="space-y-3">
      <article
        v-for="item in visibleContents"
        :key="item.contentId"
        class="flex flex-col gap-3 rounded-2xl border border-[#E8D9C9] bg-white p-3 transition hover:border-[#D96C2C]/50 sm:flex-row sm:items-center sm:gap-4"
      >
        <!-- Thumbnail -->
        <div class="h-32 w-full shrink-0 overflow-hidden rounded-xl bg-[#F7F0E6] sm:h-20 sm:w-32">
          <img
            v-if="youtubeThumbnail(item.youtubeUrl)"
            :src="youtubeThumbnail(item.youtubeUrl)"
            :alt="item.title"
            class="h-full w-full object-cover"
          />
          <div v-else class="flex h-full w-full items-center justify-center text-[#C9B8A8]">
            <i class="mdi mdi-image-outline text-3xl"></i>
          </div>
        </div>

        <!-- Title + meta -->
        <div class="min-w-0 flex-1 space-y-1">
          <h3 class="line-clamp-2 text-sm leading-snug font-black text-[#332820] sm:text-base">{{ item.title }}</h3>
          <p class="flex flex-wrap items-center gap-x-2 gap-y-0.5 text-xs font-semibold text-[#786B62]">
            <span class="inline-flex items-center gap-1.5 font-black" :class="statusText(item.status)">
              <span class="h-2 w-2 rounded-full" :class="statusDot(item.status)"></span>
              {{ statusLabel(item.status) }}
            </span>
            <span>·</span>
            <span>{{ item.contentCategoryName || item.districtName || 'เรื่องราวเมืองกาญจน์' }}</span>
            <span>·</span>
            <span>{{ formatDate(item.createdAt, { dateStyle: 'medium' }) }}</span>
          </p>
        </div>

        <!-- Actions -->
        <div class="flex shrink-0 items-center gap-2">
          <RouterLink
            :to="item.status === 'Published' ? `/contents/${item.contentId}` : `/contents/${item.contentId}?preview=mine`"
            class="rounded-full border border-[#E8D9C9] px-4 py-2 text-sm font-black text-[#332820] transition hover:border-[#D96C2C] hover:text-[#D96C2C]"
          >
            ดู
          </RouterLink>

          <template v-if="item.status !== 'Archived'">
            <RouterLink
              :to="`/my-contents/${item.contentId}/edit`"
              class="inline-flex items-center gap-1 rounded-full bg-[#FFF1E8] px-4 py-2 text-sm font-black text-[#BF5720] transition hover:bg-[#D96C2C] hover:text-white"
            >
              <i class="mdi mdi-pencil-outline"></i>
              <span>แก้ไข</span>
            </RouterLink>

            <button
              type="button"
              class="flex h-9 w-9 cursor-pointer items-center justify-center rounded-full text-[#786B62] transition hover:bg-rose-50 hover:text-rose-600 focus-visible:ring-4 focus-visible:ring-rose-200 focus-visible:outline-none"
              title="ลบคอนเทนต์"
              aria-label="ลบคอนเทนต์"
              @click="emit('delete-content', item)"
            >
              <i class="mdi mdi-trash-can-outline text-xl"></i>
            </button>
          </template>
        </div>
      </article>
    </div>
  </div>
</template>