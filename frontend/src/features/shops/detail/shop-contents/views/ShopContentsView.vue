<script setup lang="ts">
import { youtubeThumbnail } from '@/shared/utils/youtube'
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getShop, type Shop } from '@/features/shops/api'
import {
  getContentCategories,
  getPublicContents,
  type ContentCategory,
  type PublicContent,
} from '@/features/contents/api'
import { push } from 'notivue'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { formatDate } from '@/shared/utils/formatDate'
import { vReveal } from '@/shared/directives/reveal'

const route = useRoute()
const router = useRouter()

const shop = ref<Shop | null>(null)
const contents = ref<PublicContent[]>([])
const categories = ref<ContentCategory[]>([])
const loading = ref(true)

const selectedCategory = ref<string | null>(null)
const searchQuery = ref('')
type SortKey = 'newest' | 'oldest' | 'title'
const sortBy = ref<SortKey>('newest')
const sortChoices: { value: SortKey; label: string }[] = [
  { value: 'newest', label: 'ล่าสุด' },
  { value: 'oldest', label: 'เก่าสุด' },
  { value: 'title', label: 'ชื่อ A-Z' },
]

// maxresdefault (16:9, no letterbox bars) does not exist for every video; fall back to hqdefault
function fallbackThumb(event: Event, youtubeUrl?: string | null) {
  const img = event.target as HTMLImageElement
  const fallback = youtubeThumbnail(youtubeUrl)
  if (fallback && img.src !== fallback) img.src = fallback
}

function clearFilters() {
  searchQuery.value = ''
  selectedCategory.value = null
}

const apiOrigin = (import.meta.env.VITE_API_URL ?? 'https://localhost:7289/api').replace(
  /\/api$/,
  '',
)

function imageUrl(url?: string) {
  return url?.startsWith('/') ? `${apiOrigin}${url}` : url
}

const shopArea = computed(() => {
  const subDistrict = shop.value?.subDistrictName
  const district = shop.value?.districtName
  return [subDistrict ? `ต.${subDistrict}` : null, district ? `อ.${district}` : null, 'จ.กาญจนบุรี']
    .filter(Boolean)
    .join(' ')
})

const contentCategories = computed(() => [
  { id: null, name: 'ทั้งหมด', count: contents.value.length },
  ...categories.value
    .filter((category) =>
      contents.value.some((c) => c.contentCategoryId === category.contentCategoryId),
    )
    .map((category) => ({
      id: category.contentCategoryId,
      name: category.categoryName,
      count: contents.value.filter((c) => c.contentCategoryId === category.contentCategoryId)
        .length,
    })),
])

const filteredContents = computed(() => {
  let list = [...contents.value]
  if (selectedCategory.value) {
    list = list.filter((c) => c.contentCategoryId === selectedCategory.value)
  }
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase().trim()
    list = list.filter(
      (c) =>
        c.title.toLowerCase().includes(q) || (c.summary && c.summary.toLowerCase().includes(q)),
    )
  }

  if (sortBy.value === 'newest') {
    list.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
  } else if (sortBy.value === 'oldest') {
    list.sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime())
  } else if (sortBy.value === 'title') {
    list.sort((a, b) => a.title.localeCompare(b.title, 'th'))
  }

  return list
})

onMounted(async () => {
  loading.value = true
  try {
    const shopId = String(route.params.id)
    const [shopData, contentsRes, catData] = await Promise.all([
      getShop(shopId),
      getPublicContents({ shopId, page: 1, pageSize: 100 }),
      getContentCategories(),
    ])
    shop.value = shopData
    contents.value = contentsRes.items || []
    categories.value = catData || []
  } catch (error) {
    push.error({
      title: 'ไม่พบร้านค้า',
      message: getApiErrorMessage(error, 'ร้านค้านี้อาจถูกปิดการใช้งาน'),
    })
    await router.replace('/shops')
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="min-h-screen bg-[#F7F0E6] pb-24 text-[#332820]">
    <!-- BREADCRUMB -->
    <div class="border-b-2 border-[#E8D9C9] bg-[#FFF9F2] py-3.5 shadow-2xs">
      <div
        class="scrollbar-none mx-auto flex w-full max-w-[1800px] items-center gap-2 overflow-x-auto px-4 text-xs font-bold whitespace-nowrap text-[#786B62] sm:px-6 lg:px-10"
      >
        <RouterLink to="/" class="transition hover:text-[#D96C2C]">หน้าแรก</RouterLink>
        <i class="mdi mdi-chevron-right text-[#E8D9C9]"></i>
        <RouterLink to="/shops" class="transition hover:text-[#D96C2C]">ร้านค้าทั้งหมด</RouterLink>
        <i class="mdi mdi-chevron-right text-[#E8D9C9]"></i>
        <RouterLink
          v-if="shop"
          :to="`/shops/${shop.shopId}`"
          class="line-clamp-1 transition hover:text-[#D96C2C]"
          >{{ shop.shopName }}</RouterLink
        >
        <i class="mdi mdi-chevron-right text-[#E8D9C9]"></i>
        <span class="font-black text-[#D96C2C]">เรื่องราวทั้งหมด</span>
      </div>
    </div>

    <!-- HEADER -->
    <section
      v-if="shop"
      class="relative overflow-hidden border-b-2 border-[#E8D9C9] bg-gradient-to-br from-white via-[#FFF9F2] to-[#F7F0E6] py-8 sm:py-10"
    >
      <div class="pointer-events-none absolute -top-24 -right-16 h-72 w-72 rounded-full bg-[#F2A65A]/25 blur-3xl"></div>
      <div class="pointer-events-none absolute -bottom-28 left-1/3 h-64 w-64 rounded-full bg-[#D96C2C]/15 blur-3xl"></div>

      <div class="relative mx-auto w-full max-w-[1800px] px-4 sm:px-6 lg:px-10">
        <div class="flex flex-col justify-between gap-6 md:flex-row md:items-center">
          <div class="flex items-center gap-4 sm:gap-5">
            <img
              v-if="imageUrl(shop.coverImageUrl)"
              :src="imageUrl(shop.coverImageUrl)"
              :alt="shop.shopName"
              class="h-16 w-16 shrink-0 rounded-3xl border-4 border-white object-cover shadow-xl ring-4 ring-[#D96C2C]/20 sm:h-24 sm:w-24"
            />
            <div
              v-else
              class="flex h-16 w-16 shrink-0 items-center justify-center rounded-3xl border-4 border-white bg-[#FFF9F2] text-3xl text-[#D96C2C] shadow-xl ring-4 ring-[#D96C2C]/20 sm:h-24 sm:w-24 sm:text-4xl"
            >
              <i class="mdi mdi-storefront-outline"></i>
            </div>

            <div class="min-w-0 space-y-2">
              <div class="flex flex-wrap items-center gap-2">
                <span
                  class="rounded-full border border-[#E8D9C9] bg-white px-3 py-0.5 text-[11px] font-black text-[#D96C2C] shadow-2xs"
                >
                  {{ shop.categoryName || 'ร้านค้าชุมชน' }}
                </span>
                <span class="flex items-center gap-1 text-xs font-semibold text-[#786B62]">
                  <i class="mdi mdi-map-marker text-sm text-[#D96C2C]"></i>
                  {{ shopArea }}
                </span>
              </div>
              <h1
                class="text-2xl leading-tight font-black tracking-tight text-[#332820] sm:text-3xl lg:text-4xl"
              >
                เรื่องราวและคอนเทนต์ของ
                <span class="text-[#D96C2C]">{{ shop.shopName }}</span>
              </h1>
              <p class="text-xs font-semibold text-[#786B62] sm:text-sm">
                มีเรื่องราวและคอนเทนต์ที่เกี่ยวข้องทั้งหมด
                <span class="font-black text-[#D96C2C]">{{ contents.length }}</span> เรื่อง
              </p>
            </div>
          </div>

          <RouterLink
            :to="`/shops/${shop.shopId}`"
            class="inline-flex shrink-0 items-center justify-center gap-2 self-start rounded-full bg-[#D96C2C] px-6 py-3 text-xs font-black text-white shadow-lg shadow-[#D96C2C]/30 transition hover:bg-[#BF5720] focus-visible:ring-4 focus-visible:ring-[#D96C2C]/30 focus-visible:outline-none active:scale-95 sm:text-sm md:self-auto"
          >
            <i class="mdi mdi-arrow-left text-base text-white"></i>
            <span class="text-white">กลับไปหน้าร้านค้า</span>
          </RouterLink>
        </div>
      </div>
    </section>

    <!-- MAIN -->
    <main class="mx-auto w-full max-w-[1800px] space-y-6 px-4 py-6 sm:px-6 sm:py-8 lg:px-10">
      <!-- Toolbar: search, sort, categories -->
      <div
        class="space-y-4 rounded-[1.75rem] border-2 border-[#E8D9C9] bg-white p-5 shadow-md shadow-[#D96C2C]/5 sm:p-6"
      >
        <div class="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
          <div
            class="flex w-full items-center gap-1.5 rounded-2xl border-2 border-[#E8D9C9] bg-white p-1.5 shadow-sm transition focus-within:border-[#D96C2C] focus-within:shadow-md focus-within:ring-4 focus-within:ring-[#D96C2C]/15 md:max-w-md"
          >
            <span
              class="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-[#FFF9F2] text-lg text-[#D96C2C]"
            >
              <i class="mdi mdi-magnify"></i>
            </span>
            <input
              v-model="searchQuery"
              type="search"
              autocomplete="off"
              placeholder="ค้นหาเรื่องราว คอนเทนต์ ของร้านนี้..."
              aria-label="ค้นหาเรื่องราวของร้านนี้"
              class="min-w-0 flex-1 bg-transparent py-2 text-sm font-bold text-[#332820] outline-none placeholder:font-medium placeholder:text-[#A8988C] [&::-webkit-search-cancel-button]:hidden"
            />
            <button
              v-if="searchQuery"
              type="button"
              class="mr-1 flex h-7 w-7 shrink-0 cursor-pointer items-center justify-center rounded-full text-[#786B62] transition hover:bg-[#F7F0E6] hover:text-[#332820]"
              aria-label="ล้างคำค้นหา"
              @click="searchQuery = ''"
            >
              <i class="mdi mdi-close text-base"></i>
            </button>
          </div>

          <div class="flex flex-wrap items-center gap-1.5" role="radiogroup" aria-label="เรียงลำดับ">
            <span class="mr-0.5 text-xs font-bold text-[#786B62]">เรียง:</span>
            <button
              v-for="opt in sortChoices"
              :key="opt.value"
              type="button"
              role="radio"
              :aria-checked="sortBy === opt.value"
              class="shrink-0 cursor-pointer rounded-full border-2 px-3.5 py-1.5 text-xs font-black whitespace-nowrap transition select-none focus-visible:ring-4 focus-visible:ring-[#D96C2C]/20 focus-visible:outline-none active:scale-95"
              :class="
                sortBy === opt.value
                  ? 'border-[#D96C2C] bg-[#D96C2C] text-white shadow-sm'
                  : 'border-[#E8D9C9] bg-white text-[#332820] hover:border-[#D96C2C]'
              "
              @click="sortBy = opt.value"
            >
              {{ opt.label }}
            </button>
          </div>
        </div>

        <div
          v-if="contentCategories.length > 1"
          class="scrollbar-none flex items-center gap-2 overflow-x-auto border-t-2 border-[#E8D9C9] pt-4 pb-1"
        >
          <button
            v-for="cat in contentCategories"
            :key="cat.name"
            type="button"
            class="flex shrink-0 cursor-pointer items-center gap-1.5 rounded-full border-2 px-4 py-2 text-xs font-black shadow-2xs transition select-none focus-visible:ring-4 focus-visible:ring-[#D96C2C]/20 focus-visible:outline-none active:scale-95"
            :class="
              selectedCategory === cat.id
                ? 'border-[#D96C2C] bg-[#D96C2C] text-white shadow-sm'
                : 'border-[#E8D9C9] bg-[#FFF9F2] text-[#332820] hover:border-[#D96C2C]'
            "
            @click="selectedCategory = cat.id"
          >
            <span>{{ cat.name }}</span>
            <span
              class="rounded-full px-2 py-0.5 text-[11px]"
              :class="
                selectedCategory === cat.id
                  ? 'bg-white font-black text-[#D96C2C]'
                  : 'bg-[#E8D9C9]/60 font-extrabold text-[#786B62]'
              "
            >
              {{ cat.count }}
            </span>
          </button>
        </div>
      </div>

      <!-- Result line -->
      <p v-if="!loading && contents.length" class="px-1 text-xs font-bold text-[#786B62] sm:text-sm">
        แสดง <span class="font-black text-[#D96C2C]">{{ filteredContents.length }}</span> จาก
        {{ contents.length }} เรื่อง
      </p>

      <!-- Skeleton -->
      <div
        v-if="loading"
        class="grid grid-cols-1 gap-5 sm:grid-cols-2 sm:gap-6 lg:grid-cols-3 xl:grid-cols-4"
      >
        <div
          v-for="i in 8"
          :key="i"
          class="animate-pulse overflow-hidden rounded-[1.75rem] border-2 border-[#E8D9C9] bg-white shadow-xs"
        >
          <div class="aspect-16/10 w-full bg-[#F7F0E6]"></div>
          <div class="space-y-3 p-5">
            <div class="h-4 w-4/5 rounded-full bg-[#E8D9C9]"></div>
            <div class="h-3 w-full rounded-full bg-[#F7F0E6]"></div>
            <div class="h-3 w-2/3 rounded-full bg-[#F7F0E6]"></div>
          </div>
        </div>
      </div>

      <!-- Empty -->
      <div
        v-else-if="!filteredContents.length"
        class="space-y-3 rounded-[1.75rem] border-2 border-dashed border-[#E8D9C9] bg-white px-6 py-20 text-center"
      >
        <div
          class="mx-auto flex h-20 w-20 items-center justify-center rounded-full border border-[#E8D9C9] bg-[#FFF9F2] text-[#D96C2C]"
        >
          <i class="mdi mdi-text-box-remove-outline text-4xl"></i>
        </div>
        <h3 class="text-xl font-black text-[#332820]">
          {{ contents.length ? 'ไม่พบเรื่องราวที่ตรงกับตัวกรอง' : 'ยังไม่มีเรื่องราวของร้านนี้' }}
        </h3>
        <p class="mx-auto max-w-md text-xs font-semibold text-[#786B62] sm:text-sm">
          {{
            contents.length
              ? 'ลองเปลี่ยนคำค้นหาหรือเลือกหมวดหมู่อื่น'
              : 'ยังไม่มีเรื่องราวหรือบทความที่เชื่อมกับร้านค้านี้ในขณะนี้'
          }}
        </p>
        <button
          v-if="searchQuery || selectedCategory"
          type="button"
          class="mt-2 inline-flex cursor-pointer items-center gap-2 rounded-full bg-[#D96C2C] px-6 py-2.5 text-xs font-black text-white shadow-md transition hover:bg-[#BF5720] active:scale-95"
          @click="clearFilters"
        >
          <i class="mdi mdi-refresh text-base text-white"></i>
          <span class="text-white">ล้างตัวกรองทั้งหมด</span>
        </button>
      </div>

      <!-- Grid -->
      <div
        v-else
        class="grid grid-cols-1 gap-5 sm:grid-cols-2 sm:gap-6 lg:grid-cols-3 xl:grid-cols-4"
      >
        <RouterLink
          v-for="(item, idx) in filteredContents"
          :key="item.contentId"
          v-reveal
          :to="`/contents/${item.contentId}`"
          :style="{ animationDelay: `${(idx % 4) * 80}ms` }"
          class="group flex flex-col justify-between overflow-hidden rounded-[1.75rem] border-2 border-[#E8D9C9] bg-white shadow-xs transition-all duration-300 ease-out hover:-translate-y-1.5 hover:border-[#D96C2C] hover:shadow-2xl hover:shadow-[#D96C2C]/10 focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25 focus-visible:outline-none"
        >
          <div>
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
                class="absolute top-3 left-3 flex max-w-[75%] items-center gap-1.5 rounded-full border border-white/20 bg-[#D96C2C]/95 px-3 py-1 text-xs font-black text-white shadow-md backdrop-blur-md"
              >
                <i class="mdi mdi-tag-outline text-xs"></i>
                <span class="truncate">{{ item.contentCategoryName || 'เรื่องราวชุมชน' }}</span>
              </span>

              <div
                v-if="item.youtubeUrl"
                class="absolute right-3 bottom-3 flex h-9 w-9 items-center justify-center rounded-full border border-white/20 bg-slate-950/75 text-white shadow-md backdrop-blur-md transition-transform duration-300 group-hover:scale-105"
              >
                <i class="mdi mdi-play ml-0.5 text-lg text-[#F2A65A]"></i>
              </div>
            </div>

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
              <h2
                class="line-clamp-2 text-base leading-snug font-black text-[#332820] transition-colors group-hover:text-[#D96C2C] sm:text-lg"
              >
                {{ item.title }}
              </h2>
              <p
                v-if="item.summary"
                class="line-clamp-2 text-xs leading-relaxed font-medium text-[#786B62] sm:text-sm"
              >
                {{ item.summary }}
              </p>
            </div>
          </div>

          <div
            class="flex flex-wrap items-center justify-between gap-x-3 gap-y-1.5 border-t border-[#E8D9C9] bg-[#FFF9F2] px-5 py-3.5 text-xs font-bold text-[#786B62]"
          >
            <span class="flex items-center gap-1.5 text-[#332820]">
              <i class="mdi mdi-calendar-blank-outline text-sm text-[#D96C2C]"></i>
              {{ formatDate(item.publishedAt || item.createdAt, { day: 'numeric', month: 'short', year: 'numeric' }, 'เรื่องราวกาญจนบุรี') }}
            </span>
            <span class="flex items-center gap-1 text-[#D96C2C] transition group-hover:text-[#BF5720]">
              <i class="mdi mdi-map-marker text-sm"></i>
              <span>{{ item.districtName ? `อ.${item.districtName}` : 'กาญจนบุรี' }}</span>
            </span>
          </div>
        </RouterLink>
      </div>
    </main>
  </div>
</template>

<style scoped>
.scrollbar-none::-webkit-scrollbar {
  display: none;
}
.scrollbar-none {
  -ms-overflow-style: none;
  scrollbar-width: none;
}
</style>
