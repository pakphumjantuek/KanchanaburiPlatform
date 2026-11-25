<script setup lang="ts">
import { formatDate } from '@/shared/utils/formatDate'
import { youtubeThumbnail } from '@/shared/utils/youtube'
import { computed, onMounted, ref } from 'vue'
import {
  archiveMyContent,
  getOwnedShopContents,
  getPublicContents,
  type UserContent,
  type PublicContent,
} from '@/features/contents/api'
import { getMyShop, type Shop } from '@/features/merchant/api/shopApi'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'

const shop = ref<Shop | null>(null)
const contents = ref<UserContent[]>([])
const mentionedContents = ref<PublicContent[]>([])
const loading = ref(true)
const searchQuery = ref('')
const selectedTab = ref<'all' | 'Published' | 'Pending'>('all')
const viewMode = ref<'cards' | 'table'>('cards')
const swal = useSwal()

const publishedCount = computed(
  () => contents.value.filter((c) => c.status === 'Published').length,
)
const pendingCount = computed(
  () => contents.value.filter((c) => c.status !== 'Published').length,
)

const filteredContents = computed(() => {
  let list = contents.value

  // Status Tab Filter
  if (selectedTab.value === 'Published') {
    list = list.filter((c) => c.status === 'Published')
  } else if (selectedTab.value === 'Pending') {
    list = list.filter((c) => c.status !== 'Published')
  }

  // Search Query
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase().trim()
    list = list.filter(
      (c) =>
        c.title.toLowerCase().includes(q) ||
        (c.summary && c.summary.toLowerCase().includes(q)),
    )
  }

  return list
})

async function load() {
  loading.value = true
  try {
    const myShop = await getMyShop()
    shop.value = myShop

    const [owned, mentioned] = await Promise.all([
      getOwnedShopContents({ page: 1, pageSize: 100 }),
      getPublicContents({ shopId: myShop.shopId, contentType: 'User', page: 1, pageSize: 100 }),
    ])
    contents.value = (owned.items || []).filter((item) => item.status !== 'Archived')
    mentionedContents.value = mentioned.items || []
  } catch (error) {
    await swal.error(
      'โหลดข้อมูลคอนเทนต์ไม่สำเร็จ',
      getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'),
    )
  } finally {
    loading.value = false
  }
}

async function removeContent(item: UserContent) {
  const result = await swal.confirm(
    `ลบคอนเทนต์ "${item.title}"?`,
    'เรื่องราวนี้จะไม่แสดงต่อสาธารณะบนแพลตฟอร์มอีกต่อไป',
  )
  if (!result.isConfirmed) return
  try {
    await archiveMyContent(item.contentId)
    contents.value = contents.value.filter((c) => c.contentId !== item.contentId)
    await swal.success('ลบคอนเทนต์เรียบร้อยแล้ว')
  } catch (error) {
    await swal.error('ลบไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  }
}

onMounted(load)
</script>

<template>
  <div class="mx-auto w-full max-w-7xl space-y-6 pb-16 text-slate-900">
    <!-- Skeleton Loading -->
    <div v-if="loading" class="space-y-4">
      <div class="h-28 w-full animate-pulse rounded-2xl bg-white border-2 border-slate-200"></div>
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div v-for="i in 3" :key="i" class="h-24 animate-pulse rounded-2xl bg-white border-2 border-slate-200"></div>
      </div>
      <div class="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
        <div
          v-for="i in 6"
          :key="i"
          class="h-72 animate-pulse rounded-2xl bg-white border-2 border-slate-200"
        ></div>
      </div>
    </div>

    <template v-else-if="shop">
      <!-- 1. Header Banner Card (High Contrast, Bold, Clear) -->
      <div
        class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 rounded-2xl border-2 border-slate-200 bg-white p-6 shadow-xs"
      >
        <div class="flex items-center gap-4">
          <div
            class="flex h-12 w-12 items-center justify-center rounded-2xl bg-emerald-700 text-white shadow-sm shrink-0"
          >
            <i class="mdi mdi-text-box-multiple-outline text-2xl text-white"></i>
          </div>
          <div>
            <div class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full bg-emerald-100 border border-emerald-300 text-xs font-black text-emerald-950 mb-1">
              <i class="mdi mdi-bullhorn text-xs text-emerald-800"></i>
              <span>ศูนย์คอนเทนต์ • ประชาสัมพันธ์และเรื่องราวร้านค้า</span>
            </div>
            <div class="flex items-center gap-3">
              <h1 class="text-2xl sm:text-3xl font-black text-slate-950 tracking-tight">
                จัดการคอนเทนต์ร้านค้า
              </h1>
              <span
                class="rounded-xl bg-slate-900 text-white px-3 py-0.5 text-xs font-black"
              >
                {{ contents.length }} บทความ
              </span>
            </div>
            <p class="text-xs sm:text-sm text-slate-700 font-bold mt-0.5">
              แบ่งปันเรื่องราว วัฒนธรรมท้องถิ่น และวิดีโอแนะนำร้านค้าให้ผู้เข้าชมรู้จักร้านของคุณ
            </p>
          </div>
        </div>

        <RouterLink
          to="/my-shop/contents/new"
          class="inline-flex items-center justify-center gap-2 rounded-xl bg-emerald-700 hover:bg-emerald-800 px-5 py-2.5 text-xs sm:text-sm font-bold text-white shadow-xs transition active:scale-95 border border-emerald-700 cursor-pointer self-start sm:self-auto select-none"
        >
          <i class="mdi mdi-plus-circle text-base text-white"></i>
          <span>เขียนบทความใหม่</span>
        </RouterLink>
      </div>

      <!-- 2. Overview Metrics Cards (High Contrast, Bold Numbers) -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <!-- Total Contents Card -->
        <div class="rounded-2xl border-2 border-slate-200 bg-white p-5 shadow-xs flex items-center justify-between">
          <div>
            <span class="text-xs font-bold text-slate-600 uppercase tracking-wide">เรื่องราวทั้งหมด</span>
            <p class="text-2xl font-black text-slate-950 mt-1">{{ contents.length }} บทความ</p>
            <span class="text-xs font-bold text-slate-500">บทความที่สร้างโดยร้านของคุณ</span>
          </div>
          <div class="h-12 w-12 rounded-2xl bg-slate-100 flex items-center justify-center text-slate-700 shrink-0">
            <i class="mdi mdi-text-box-outline text-2xl"></i>
          </div>
        </div>

        <!-- Published Card -->
        <div class="rounded-2xl border-2 border-slate-200 bg-white p-5 shadow-xs flex items-center justify-between">
          <div>
            <span class="text-xs font-bold text-emerald-800 uppercase tracking-wide">เผยแพร่สาธารณะ</span>
            <p class="text-2xl font-black text-emerald-800 mt-1">{{ publishedCount }} บทความ</p>
            <span class="text-xs font-bold text-emerald-700">แสดงผลบนเว็บไซต์ให้ผู้เยี่ยมชมอ่าน</span>
          </div>
          <div class="h-12 w-12 rounded-2xl bg-emerald-100 flex items-center justify-center text-emerald-800 shrink-0 border border-emerald-300">
            <i class="mdi mdi-check-circle text-2xl"></i>
          </div>
        </div>

        <!-- Mentioned by Users Card -->
        <div class="rounded-2xl border-2 border-slate-200 bg-white p-5 shadow-xs flex items-center justify-between">
          <div>
            <span class="text-xs font-bold text-teal-800 uppercase tracking-wide">ผู้ใช้เล่าถึงร้านค้า</span>
            <p class="text-2xl font-black text-teal-900 mt-1">{{ mentionedContents.length }} เรื่อง</p>
            <span class="text-xs font-bold text-teal-700">รีวิวหรือบทความจากลูกค้า</span>
          </div>
          <div class="h-12 w-12 rounded-2xl bg-teal-100 flex items-center justify-center text-teal-800 shrink-0 border border-teal-300">
            <i class="mdi mdi-account-voice text-2xl"></i>
          </div>
        </div>
      </div>

      <!-- 3. Filter Bar: Status Tabs + Search Input + View Mode Switcher -->
      <div
        v-if="contents.length > 0"
        class="rounded-2xl border-2 border-slate-200 bg-slate-100/90 p-2.5 shadow-xs flex flex-col md:flex-row md:items-center justify-between gap-3"
      >
        <!-- Status Tabs -->
        <div class="flex items-center gap-1.5 overflow-x-auto pb-1 md:pb-0 scrollbar-none flex-1">
          <button
            type="button"
            class="flex items-center gap-2 rounded-xl px-4 py-2 text-xs font-black transition shrink-0 cursor-pointer select-none"
            :class="
              selectedTab === 'all'
                ? 'bg-emerald-800 text-white shadow-sm'
                : 'bg-white text-slate-900 border border-slate-200 hover:bg-slate-50'
            "
            @click="selectedTab = 'all'"
          >
            <span>ทั้งหมด</span>
            <span
              class="rounded-full px-2 py-0.2 text-[11px] font-black"
              :class="selectedTab === 'all' ? 'bg-white/25 text-white' : 'bg-slate-900 text-white'"
            >
              {{ contents.length }}
            </span>
          </button>

          <button
            type="button"
            class="flex items-center gap-2 rounded-xl px-4 py-2 text-xs font-black transition shrink-0 cursor-pointer select-none"
            :class="
              selectedTab === 'Published'
                ? 'bg-emerald-800 text-white shadow-sm'
                : 'bg-white text-slate-900 border border-slate-200 hover:bg-slate-50'
            "
            @click="selectedTab = 'Published'"
          >
            <span>เผยแพร่แล้ว</span>
            <span
              class="rounded-full px-2 py-0.2 text-[11px] font-black"
              :class="selectedTab === 'Published' ? 'bg-white/25 text-white' : 'bg-emerald-700 text-white'"
            >
              {{ publishedCount }}
            </span>
          </button>

          <button
            type="button"
            class="flex items-center gap-2 rounded-xl px-4 py-2 text-xs font-black transition shrink-0 cursor-pointer select-none"
            :class="
              selectedTab === 'Pending'
                ? 'bg-emerald-800 text-white shadow-sm'
                : 'bg-white text-slate-900 border border-slate-200 hover:bg-slate-50'
            "
            @click="selectedTab = 'Pending'"
          >
            <span>รอตรวจสอบ / ร่าง</span>
            <span
              class="rounded-full px-2 py-0.2 text-[11px] font-black"
              :class="selectedTab === 'Pending' ? 'bg-white/25 text-white' : 'bg-amber-500 text-white'"
            >
              {{ pendingCount }}
            </span>
          </button>
        </div>

        <div class="flex items-center gap-2">
          <!-- Search Input -->
          <div class="relative min-w-[220px]">
            <input
              v-model="searchQuery"
              type="text"
              placeholder="ค้นหาชื่อเรื่องหรือเนื้อหา..."
              class="w-full rounded-xl border-2 border-slate-300 bg-white pl-9 pr-8 py-2 text-xs font-bold text-slate-950 placeholder:text-slate-500 focus:border-emerald-700 transition"
            />
            <i
              class="mdi mdi-magnify absolute left-3 top-1/2 -translate-y-1/2 text-slate-500 text-base"
            ></i>
            <button
              v-if="searchQuery"
              type="button"
              class="absolute right-2.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-800 cursor-pointer"
              @click="searchQuery = ''"
            >
              <i class="mdi mdi-close-circle text-base"></i>
            </button>
          </div>

          <!-- View Mode Toggle -->
          <div class="flex items-center rounded-xl bg-slate-200 p-1 border border-slate-300 shrink-0">
            <button
              type="button"
              class="flex items-center gap-1.5 rounded-lg px-2.5 py-1 text-xs font-black transition cursor-pointer"
              :class="
                viewMode === 'cards'
                  ? 'bg-white text-slate-950 shadow-xs'
                  : 'text-slate-700 hover:text-slate-950'
              "
              @click="viewMode = 'cards'"
              title="มุมมองแบบการ์ด"
            >
              <i class="mdi mdi-view-grid-outline text-sm"></i>
              <span class="hidden sm:inline">การ์ด</span>
            </button>
            <button
              type="button"
              class="flex items-center gap-1.5 rounded-lg px-2.5 py-1 text-xs font-black transition cursor-pointer"
              :class="
                viewMode === 'table'
                  ? 'bg-white text-slate-950 shadow-xs'
                  : 'text-slate-700 hover:text-slate-950'
              "
              @click="viewMode = 'table'"
              title="มุมมองแบบตาราง"
            >
              <i class="mdi mdi-table text-sm"></i>
              <span class="hidden sm:inline">ตาราง</span>
            </button>
          </div>
        </div>
      </div>

      <!-- 4. Empty State: No Contents Created Yet -->
      <div
        v-if="contents.length === 0"
        class="my-8 flex flex-col items-center justify-center rounded-2xl border-2 border-dashed border-slate-300 bg-white py-16 px-6 text-center shadow-xs space-y-3"
      >
        <div
          class="flex h-16 w-16 items-center justify-center rounded-2xl bg-emerald-100 text-emerald-800 mb-2"
        >
          <i class="mdi mdi-text-box-plus-outline text-3xl"></i>
        </div>
        <h2 class="text-xl font-black text-slate-950">ยังไม่มีคอนเทนต์หรือเรื่องราวของร้านค้า</h2>
        <p class="max-w-md text-xs sm:text-sm text-slate-700 font-bold leading-relaxed">
          เริ่มต้นเขียนเรื่องราว เล่าความประทับใจ หรือลงวิดีโอแนะนำร้านค้าเพื่อให้ผู้เยี่ยมชมรู้จักร้านของคุณมากยิ่งขึ้น
        </p>
        <RouterLink
          to="/my-shop/contents/new"
          class="mt-4 inline-flex items-center gap-2 rounded-xl bg-emerald-700 hover:bg-emerald-800 px-6 py-2.5 text-xs sm:text-sm font-bold text-white shadow-xs transition active:scale-95 border border-emerald-700 cursor-pointer"
        >
          <i class="mdi mdi-plus-circle text-base text-white"></i>
          <span>เขียนบทความแรกของร้านค้า</span>
        </RouterLink>
      </div>

      <!-- 5. Empty State: Filter / Search Returns Zero -->
      <div
        v-else-if="filteredContents.length === 0"
        class="rounded-2xl border-2 border-slate-200 bg-white p-12 text-center text-slate-700 shadow-xs space-y-3"
      >
        <div class="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-slate-100 text-slate-700">
          <i class="mdi mdi-magnify-remove text-3xl"></i>
        </div>
        <div>
          <p class="font-black text-slate-950 text-base">ไม่พบคอนเทนต์ที่ตรงกับคำค้นหาหรือตัวกรองนี้</p>
          <p class="text-xs font-bold text-slate-500 mt-1">ลองล้างคำค้นหาหรือเลือกแท็บอื่น</p>
        </div>
        <button
          type="button"
          class="inline-flex items-center gap-1.5 rounded-xl bg-emerald-700 hover:bg-emerald-800 text-white px-4 py-2 text-xs font-bold transition shadow-xs cursor-pointer select-none"
          @click="
            () => {
              searchQuery = ''
              selectedTab = 'all'
            }
          "
        >
          <i class="mdi mdi-filter-remove-outline text-sm"></i>
          <span>ล้างตัวกรองและค้นหาใหม่</span>
        </button>
      </div>

      <!-- 6. View Mode: CARDS -->
      <div v-else-if="viewMode === 'cards'" class="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
        <article
          v-for="item in filteredContents"
          :key="item.contentId"
          class="group flex flex-col overflow-hidden rounded-2xl border-2 border-slate-200 bg-white shadow-xs transition-all duration-200 hover:border-slate-400 hover:shadow-md justify-between"
        >
          <div>
            <!-- Banner Image / YouTube Thumbnail -->
            <div class="relative aspect-16/10 w-full overflow-hidden bg-slate-900">
              <img
                v-if="youtubeThumbnail(item.youtubeUrl)"
                :src="youtubeThumbnail(item.youtubeUrl)"
                :alt="item.title"
                class="h-full w-full object-cover transition-transform duration-500 group-hover:scale-105"
              />
              <div
                v-else
                class="flex h-full w-full items-center justify-center bg-gradient-to-br from-emerald-800 to-slate-900 text-white"
              >
                <i class="mdi mdi-text-box-outline text-5xl opacity-40"></i>
              </div>

              <!-- Category Badge -->
              <div class="absolute top-3 left-3">
                <span
                  class="inline-flex items-center rounded-lg bg-slate-900/90 backdrop-blur-xs px-2.5 py-1 text-[11px] font-black text-white shadow-xs"
                >
                  {{ item.contentCategoryName || 'เรื่องราวร้านค้า' }}
                </span>
              </div>

              <!-- Status Badge (Solid Vivid High Contrast) -->
              <div class="absolute top-3 right-3">
                <span
                  class="inline-flex items-center gap-1 rounded-lg px-2.5 py-1 text-[11px] font-bold shadow-xs text-white"
                  :class="
                    item.status === 'Published'
                      ? 'bg-emerald-600'
                      : 'bg-amber-500'
                  "
                >
                  <i
                    :class="[
                      'mdi text-xs',
                      item.status === 'Published' ? 'mdi-check-circle' : 'mdi-clock-outline',
                    ]"
                  ></i>
                  <span>{{ item.status === 'Published' ? 'เผยแพร่แล้ว' : 'รอตรวจสอบ' }}</span>
                </span>
              </div>
            </div>

            <!-- Content Card Body -->
            <div class="p-5 space-y-2">
              <h2
                class="text-base font-black text-slate-950 group-hover:text-emerald-800 transition line-clamp-2 leading-snug"
              >
                {{ item.title }}
              </h2>
              <p
                v-if="item.summary"
                class="text-xs font-medium text-slate-700 line-clamp-2 leading-relaxed"
              >
                {{ item.summary }}
              </p>
              <div class="text-[11px] font-bold text-slate-600 flex items-center gap-1.5 pt-1">
                <i class="mdi mdi-calendar-clock text-slate-500 text-sm"></i>
                <span>สร้างเมื่อ: {{ formatDate(item.createdAt, undefined, '') }}</span>
              </div>
            </div>
          </div>

          <!-- Card Footer Actions -->
          <div class="p-5 pt-0">
            <div class="flex items-center justify-between border-t-2 border-slate-100 pt-3 text-xs">
              <RouterLink
                :to="`/contents/${item.contentId}`"
                target="_blank"
                class="inline-flex items-center gap-1 font-bold text-slate-700 hover:text-emerald-800 transition"
              >
                <span>ดูบทความ</span>
                <i class="mdi mdi-open-in-new text-xs text-emerald-700"></i>
              </RouterLink>

              <div class="flex items-center gap-2">
                <RouterLink
                  :to="`/my-shop/contents/${item.contentId}/edit`"
                  class="inline-flex items-center gap-1 rounded-xl border border-slate-300 bg-white hover:bg-slate-50 px-3 py-1.5 font-bold text-slate-800 transition shadow-2xs"
                >
                  <i class="mdi mdi-pencil-outline text-xs text-emerald-700"></i>
                  <span>แก้ไข</span>
                </RouterLink>

                <button
                  type="button"
                  class="inline-flex items-center gap-1 rounded-xl border border-rose-300 bg-white hover:bg-rose-50 px-3 py-1.5 font-bold text-rose-700 transition shadow-2xs cursor-pointer"
                  @click="removeContent(item)"
                >
                  <i class="mdi mdi-trash-can-outline text-xs"></i>
                  <span>ลบ</span>
                </button>
              </div>
            </div>
          </div>
        </article>
      </div>

      <!-- 7. View Mode: TABLE (Fast Scanning) -->
      <div v-else-if="viewMode === 'table'" class="rounded-2xl border-2 border-slate-200 bg-white shadow-sm overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-left border-collapse text-sm">
            <thead>
              <tr class="border-b-2 border-slate-200 bg-slate-100 text-xs font-black text-slate-900 uppercase tracking-wider">
                <th class="py-4 px-5">บทความ</th>
                <th class="py-4 px-5">หมวดหมู่</th>
                <th class="py-4 px-5">วันที่สร้าง</th>
                <th class="py-4 px-5">สถานะ</th>
                <th class="py-4 px-5 text-right">ดำเนินการ</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-200 font-bold text-slate-900">
              <tr
                v-for="item in filteredContents"
                :key="item.contentId"
                class="hover:bg-slate-50 transition"
              >
                <!-- Title & Thumbnail -->
                <td class="py-4 px-5 max-w-[320px]">
                  <div class="flex items-center gap-3">
                    <div class="h-12 w-16 rounded-lg bg-slate-900 overflow-hidden shrink-0 flex items-center justify-center">
                      <img
                        v-if="youtubeThumbnail(item.youtubeUrl)"
                        :src="youtubeThumbnail(item.youtubeUrl)"
                        :alt="item.title"
                        class="h-full w-full object-cover"
                      />
                      <i v-else class="mdi mdi-text-box text-white text-xl opacity-50"></i>
                    </div>
                    <div class="min-w-0">
                      <p class="text-sm font-black text-slate-950 truncate">{{ item.title }}</p>
                      <p class="text-xs font-medium text-slate-600 truncate mt-0.5">{{ item.summary }}</p>
                    </div>
                  </div>
                </td>

                <!-- Category -->
                <td class="py-4 px-5">
                  <span class="rounded-lg bg-slate-100 border border-slate-200 text-slate-800 px-2.5 py-1 text-xs font-bold">
                    {{ item.contentCategoryName || 'เรื่องราวร้านค้า' }}
                  </span>
                </td>

                <!-- Created Date -->
                <td class="py-4 px-5 text-xs text-slate-700 font-bold">
                  {{ formatDate(item.createdAt, undefined, '') }}
                </td>

                <!-- Status -->
                <td class="py-4 px-5">
                  <span
                    class="inline-flex items-center gap-1 rounded-xl px-2.5 py-1 text-xs font-bold text-white shadow-2xs"
                    :class="
                      item.status === 'Published'
                        ? 'bg-emerald-600'
                        : 'bg-amber-500'
                    "
                  >
                    <i
                      :class="[
                        'mdi text-xs',
                        item.status === 'Published' ? 'mdi-check-circle' : 'mdi-clock-outline',
                      ]"
                    ></i>
                    <span>{{ item.status === 'Published' ? 'เผยแพร่แล้ว' : 'รอตรวจสอบ' }}</span>
                  </span>
                </td>

                <!-- Actions -->
                <td class="py-4 px-5 text-right">
                  <div class="flex items-center justify-end gap-2">
                    <RouterLink
                      :to="`/contents/${item.contentId}`"
                      target="_blank"
                      class="rounded-xl border border-slate-300 bg-white hover:bg-slate-50 px-3 py-1.5 text-xs font-bold text-slate-800 transition"
                      title="ดูหน้าร้านจริง"
                    >
                      ดูบทความ
                    </RouterLink>

                    <RouterLink
                      :to="`/my-shop/contents/${item.contentId}/edit`"
                      class="rounded-xl bg-emerald-700 hover:bg-emerald-800 text-white px-3 py-1.5 text-xs font-bold transition shadow-2xs"
                    >
                      แก้ไข
                    </RouterLink>

                    <button
                      type="button"
                      class="rounded-xl border-2 border-rose-300 bg-white hover:bg-rose-50 text-rose-700 px-3 py-1.5 text-xs font-bold transition cursor-pointer"
                      @click="removeContent(item)"
                    >
                      ลบ
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- 8. Section: Users Mentioning This Shop (Clean, Modern User Stories) -->
      <section class="rounded-2xl border-2 border-slate-200 bg-white p-6 shadow-xs space-y-4">
        <div class="flex items-center justify-between border-b-2 border-slate-100 pb-3">
          <div class="flex items-center gap-2.5">
            <span class="flex h-8 w-8 items-center justify-center rounded-xl bg-teal-100 text-teal-800">
              <i class="mdi mdi-account-voice text-lg"></i>
            </span>
            <div>
              <h2 class="text-base font-black text-slate-950">
                เรื่องราวที่ผู้ใช้เล่าถึงร้านค้า ({{ mentionedContents.length }})
              </h2>
              <p class="text-xs font-bold text-slate-600">
                บทความและรีวิวจากผู้ใช้งานบนแพลตฟอร์มที่กล่าวถึงร้านค้าของคุณ
              </p>
            </div>
          </div>
        </div>

        <div v-if="mentionedContents.length" class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <RouterLink
            v-for="item in mentionedContents"
            :key="item.contentId"
            :to="`/contents/${item.contentId}`"
            target="_blank"
            class="group rounded-2xl border-2 border-slate-200 bg-slate-50 p-4 transition hover:border-slate-400 hover:bg-white flex flex-col justify-between"
          >
            <div>
              <h3 class="font-black text-slate-950 group-hover:text-emerald-800 transition line-clamp-2 text-sm leading-snug">
                {{ item.title }}
              </h3>
              <p class="mt-2 line-clamp-2 text-xs font-medium text-slate-700 leading-relaxed">
                {{ item.summary || 'ไม่มีบทคัดย่อ' }}
              </p>
            </div>
            <div class="mt-3 flex items-center justify-between pt-2 border-t border-slate-200 text-xs font-bold text-emerald-800">
              <span>อ่านบทความเต็ม</span>
              <i class="mdi mdi-arrow-right text-sm group-hover:translate-x-1 transition-transform"></i>
            </div>
          </RouterLink>
        </div>
        <div v-else class="py-8 text-center text-slate-500 font-bold text-xs">
          ยังไม่มีบทความจากผู้ใช้งานที่กล่าวถึงร้านนี้
        </div>
      </section>
    </template>
  </div>
</template>

<style scoped>
button,
a,
input {
  outline: none !important;
  -webkit-tap-highlight-color: transparent !important;
}

button:focus,
button:focus-visible,
button:active {
  outline: none !important;
  box-shadow: none !important;
}

.scrollbar-none::-webkit-scrollbar {
  display: none;
}
.scrollbar-none {
  -ms-overflow-style: none;
  scrollbar-width: none;
}
</style>
