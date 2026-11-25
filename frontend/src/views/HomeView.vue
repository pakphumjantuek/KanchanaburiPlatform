<script setup lang="ts">
import { youtubeThumbnail } from '@/shared/utils/youtube'
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import {
  getContentCategories,
  getDistricts,
  getPublicContents,
  type ContentCategory,
  type District,
  type PublicContent,
} from '@/features/contents/public/api/contentApi'
import { getShops } from '@/features/shops/api/shopApi'
import type { Shop } from '@/features/shops/shared/types/shop'
import type { Product } from '@/features/shops/shared/types/product'
import { getActiveDealProducts, getPublicProducts } from '@/features/shops/api/productApi'
import { toUtcMilliseconds } from '@/shared/composables/useDealCountdown'
import { dealLabel, dealPrice } from '@/shared/utils/productDeal'
import { vReveal } from '@/shared/directives/reveal'
import AnimatedNumber from '@/shared/components/AnimatedNumber.vue'
import { useAuthStore } from '@/features/auth/stores/auth'
import heroCoverImage from '@/assets/รูปปก.png'

const router = useRouter()
const auth = useAuthStore()
const searchQuery = ref('')

const loading = ref(true)
const categories = ref<ContentCategory[]>([])
const recommendedContents = ref<PublicContent[]>([])
const communityStories = ref<PublicContent[]>([])
const districts = ref<District[]>([])
const shops = ref<Shop[]>([])
const shopTotal = ref(0)
const contentTotal = ref(0)
const products = ref<Product[]>([])
const activeDealProducts = ref<Product[]>([])
const scrollY = ref(0)
const currentTime = ref(Date.now())
let dealCountdownTimer: ReturnType<typeof window.setInterval> | undefined

const visibleDealProducts = computed(() => {
  return activeDealProducts.value.filter((product) => {
    const deal = product.activeDeal
    return (
      !!deal && deal.availableQuantity > 0 && toUtcMilliseconds(deal.endsAt) > currentTime.value
    )
  })
})

// Links built from real data (categories + districts), not made-up "popular searches"
const quickLinks = computed(() => [
  ...categories.value.slice(0, 3).map((c) => ({
    label: c.categoryName,
    to: { path: '/contents', query: { categoryId: String(c.contentCategoryId) } },
  })),
  ...districts.value.slice(0, 3).map((d) => ({
    label: `อ.${d.districtName}`,
    to: { path: '/contents', query: { districtId: String(d.districtId) } },
  })),
])

// Stats come from the API; nothing is shown for a number that is zero/unknown
const routeStats = computed(() =>
  [
    {
      value: districts.value.length,
      label: 'อำเภอบนเส้นทาง',
      icon: 'mdi-map-marker-radius-outline',
    },
    { value: shopTotal.value, label: 'ร้านค้าชุมชน', icon: 'mdi-storefront-outline' },
    {
      value: contentTotal.value,
      label: 'เรื่องราวและคอนเทนต์',
      icon: 'mdi-book-open-page-variant-outline',
    },
  ]
    .filter((stat) => stat.value > 0),
)

const shopCtaLink = computed(() => (auth.isLoggedIn ? '/register-shop' : '/login'))

// How a local story becomes income (every step maps to a real feature of the platform)
const flowSteps = [
  {
    icon: 'mdi-pencil-box-outline',
    title: 'เล่าเรื่องของชุมชน',
    desc: 'ลงคอนเทนต์ที่บอกเล่าสถานที่ ภูมิปัญญา และวิถีชีวิตของกาญจนบุรี',
  },
  {
    icon: 'mdi-map-search-outline',
    title: 'ผู้คนค้นพบ',
    desc: 'ผู้เข้าชมค้นหาเรื่องราวตามหมวดหมู่ อำเภอ และแผนที่',
  },
  {
    icon: 'mdi-storefront-outline',
    title: 'เชื่อมสู่ร้านค้า',
    desc: 'คอนเทนต์เชื่อมกับร้านค้าและสินค้าท้องถิ่นที่เกี่ยวข้อง',
  },
  {
    icon: 'mdi-cash-multiple',
    title: 'สร้างรายได้สู่ชุมชน',
    desc: 'ผู้ซื้ออุดหนุนสินค้าผ่านระบบสั่งซื้อและชำระเงินออนไลน์',
  },
]

// Cycled dot colors for the "transit line" category strip.
const lineColors = ['#D96C2C', '#BF5720', '#F2A65A', '#9C4315', '#786B62']

const apiOrigin = (import.meta.env.VITE_API_URL ?? 'https://localhost:7289/api').replace(
  /\/api$/,
  '',
)

function resolveImageUrl(url?: string) {
  if (!url || !url.trim()) return ''
  if (url.startsWith('http://') || url.startsWith('https://')) return url
  return url.startsWith('/') ? `${apiOrigin}${url}` : `${apiOrigin}/${url}`
}

// images that failed to load fall back to the icon placeholder instead of showing alt text
const failedCovers = reactive<Record<string, boolean>>({})

function getShopCover(shop: Shop, _idx: number) {
  return failedCovers[shop.shopId] ? '' : resolveImageUrl(shop.coverImageUrl)
}

function getProductCover(prod: Product, _idx: number) {
  return failedCovers[prod.productId] ? '' : resolveImageUrl(prod.imageUrl)
}

// maxresdefault (16:9, no letterbox bars) does not exist for every video; fall back to hqdefault
function fallbackThumb(event: Event, youtubeUrl?: string | null) {
  const img = event.target as HTMLImageElement
  const fallback = youtubeThumbnail(youtubeUrl)
  if (fallback && img.src !== fallback) img.src = fallback
}

function discountLabel(product: Product) {
  const deal = product.activeDeal

  if (!deal) {
    return ''
  }

  return deal.discountType === 'Percent'
    ? `ลด ${deal.discountValue}%`
    : `ลด ฿${Number(deal.discountValue).toLocaleString('th-TH')}`
}

function remainingDealTime(endsAt: string) {
  const seconds = Math.max(0, Math.floor((toUtcMilliseconds(endsAt) - currentTime.value) / 1000))
  const days = Math.floor(seconds / 86400)
  const hours = Math.floor((seconds % 86400) / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  const remainingSeconds = seconds % 60
  const clock = [hours, minutes, remainingSeconds]
    .map((value) => String(value).padStart(2, '0'))
    .join(':')

  return days > 0 ? `${days} วัน ${clock}` : clock
}

function handleSearch(queryText?: string) {
  const q = queryText ?? searchQuery.value
  if (q && q.trim()) {
    router.push({ path: '/contents', query: { search: q.trim() } })
  } else {
    router.push('/contents')
  }
}

function handleScroll() {
  scrollY.value = window.scrollY
}

function scrollToContent() {
  const hero = document.getElementById('home-hero')
  const top = hero ? hero.offsetTop + hero.offsetHeight : window.innerHeight
  const nav = document.querySelector('header')?.getBoundingClientRect().height ?? 80
  window.scrollTo({ top: top - nav + 8, behavior: 'smooth' })
}

async function loadHomeData() {
  loading.value = true
  try {
    const [catRes, contentRes, distRes, shopRes] = await Promise.allSettled([
      getContentCategories(),
      getPublicContents({ page: 1, pageSize: 8 }),
      getDistricts(),
      getShops(),
    ])

    if (catRes.status === 'fulfilled') {
      categories.value = catRes.value || []
    }

    if (contentRes.status === 'fulfilled' && contentRes.value) {
      const items = contentRes.value.items || []
      contentTotal.value = contentRes.value.totalCount ?? items.length
      recommendedContents.value = items.slice(0, 8)
      communityStories.value = items.slice(8)
    }

    if (distRes.status === 'fulfilled') {
      districts.value = distRes.value || []
    }

    if (shopRes.status === 'fulfilled') {
      const val = shopRes.value
      const items = 'items' in val ? val.items : val
      shopTotal.value =
        'totalCount' in val ? (val.totalCount ?? (items || []).length) : (items || []).length
      shops.value = (items || []).slice(0, 8)
    }

    const [productResult, dealProductResult] = await Promise.allSettled([
      getPublicProducts(),
      getActiveDealProducts(12),
    ])

    // Out-of-stock products are not shown; filter before slicing so the section still fills up to 8
    products.value =
      productResult.status === 'fulfilled'
        ? productResult.value.filter((product) => product.quantity > 0).slice(0, 8)
        : []
    activeDealProducts.value =
      dealProductResult.status === 'fulfilled' ? dealProductResult.value : []
  } catch {
    /* ignore */
  }
  loading.value = false
}

onMounted(() => {
  void loadHomeData()
  dealCountdownTimer = window.setInterval(() => {
    currentTime.value = Date.now()
  }, 1000)
  window.addEventListener('scroll', handleScroll, { passive: true })
})

onUnmounted(() => {
  if (dealCountdownTimer) {
    window.clearInterval(dealCountdownTimer)
  }
  window.removeEventListener('scroll', handleScroll)
})
</script>

<template>
  <div class="min-h-screen bg-[#F7F0E6] text-[#332820] font-body">
    <!-- 1. HERO — immersive photo, glass search, live highlights -->
    <section
      id="home-hero"
      class="relative isolate overflow-hidden bg-[#171412] text-white min-h-[calc(100svh-4rem-2px)] sm:min-h-[calc(100svh-5rem-2px)] flex items-center"
    >
      <!-- Photo (gentle parallax) -->
      <img
        :src="heroCoverImage"
        alt="สะพานข้ามแม่น้ำแคว กาญจนบุรี"
        class="absolute inset-0 -z-30 h-full w-full object-cover object-center pointer-events-none will-change-transform"
        :style="{ transform: `translateY(${Math.min(scrollY, 600) * 0.25}px) scale(1.08)` }"
      />
      <!-- Warm readability overlays -->
      <div
        class="absolute inset-0 -z-20 bg-gradient-to-r from-[#171412]/95 via-[#171412]/75 to-[#171412]/45"
      ></div>
      <div
        class="absolute inset-0 -z-20 bg-gradient-to-t from-[#171412]/80 via-[#171412]/15 to-[#171412]/45"
      ></div>
      <!-- Brand glows -->
      <div
        class="pointer-events-none absolute -top-24 -left-24 -z-10 h-96 w-96 rounded-full bg-[#D96C2C]/30 blur-3xl"
      ></div>
      <div
        class="pointer-events-none absolute -right-20 bottom-10 -z-10 h-80 w-80 rounded-full bg-[#F2A65A]/20 blur-3xl"
      ></div>

      <div
        class="hero-inner relative mx-auto grid w-full max-w-[1800px] grid-cols-1 items-center gap-10 px-4 pt-6 pb-20 sm:px-6 sm:pt-16 sm:pb-28 lg:grid-cols-12 lg:gap-12 lg:px-10 lg:pt-16 lg:pb-28"
      >
        <!-- LEFT: message + search -->
        <div class="min-w-0 space-y-4 sm:space-y-6 lg:col-span-7">
          <p
            class="hero-rise inline-flex max-w-full items-start gap-2 rounded-2xl border border-white/25 bg-black/40 px-4 py-2 text-xs font-bold text-[#FFD9A8] backdrop-blur-md sm:items-center sm:rounded-full sm:text-sm"
          >
            <i class="mdi mdi-creation mt-0.5 text-base text-[#F2A65A] sm:mt-0"></i>
            <span
              >แพลตฟอร์มดิจิทัลเพื่อยกระดับคอนเทนต์ท้องถิ่นและเศรษฐกิจสร้างสรรค์
              จังหวัดกาญจนบุรี</span
            >
          </p>

          <h1
            class="hero-rise hero-title font-display text-4xl leading-[1.35] font-black tracking-tight text-white drop-shadow-2xl sm:text-5xl lg:text-5xl xl:text-6xl"
            style="animation-delay: 80ms"
          >
            <span class="inline-block">ยกระดับ</span>
            <span class="inline-block">คอนเทนต์ท้องถิ่น</span>
            <span
              class="block bg-gradient-to-r from-[#F7C07A] via-[#F2A65A] to-[#FFD9A8] bg-clip-text pt-1 pb-3 text-transparent"
            >
              <span class="inline-block">สู่เศรษฐกิจสร้างสรรค์</span>
              <span class="inline-block">กาญจนบุรี</span>
            </span>
          </h1>

          <p
            class="hero-rise hero-lead max-w-2xl text-base leading-relaxed font-medium text-[#F7F0E6] drop-shadow-lg sm:text-lg"
            style="animation-delay: 160ms"
          >
            รวมเรื่องราว วิถีชุมชน ร้านค้า และสินค้าท้องถิ่นของกาญจนบุรีไว้ในที่เดียว
            ให้เรื่องเล่าของชุมชนกลายเป็นโอกาสสร้างรายได้
          </p>

          <!-- Search pill -->
          <form
            class="hero-rise flex max-w-2xl items-center gap-2 rounded-full bg-white p-2 shadow-2xl shadow-black/30 ring-4 ring-white/20 transition focus-within:ring-[#F2A65A]/50"
            style="animation-delay: 240ms"
            role="search"
            @submit.prevent="handleSearch()"
          >
            <span
              class="ml-1 flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-[#FFF9F2] text-xl text-[#D96C2C]"
            >
              <i class="mdi mdi-magnify"></i>
            </span>
            <label for="home-search" class="sr-only">ค้นหาข้อมูล</label>
            <input
              id="home-search"
              v-model="searchQuery"
              type="search"
              autocomplete="off"
              placeholder="ค้นหาสถานที่ เรื่องราว อาหาร ร้านค้า..."
              class="min-w-0 flex-1 bg-transparent px-1 py-2 text-sm font-bold text-[#332820] outline-none placeholder:font-medium placeholder:text-[#A8988C] sm:text-base [&::-webkit-search-cancel-button]:hidden"
            />
            <button
              type="submit"
              class="flex h-12 shrink-0 cursor-pointer items-center justify-center gap-2 rounded-full bg-gradient-to-r from-[#D96C2C] to-[#BF5720] px-5 text-sm font-black text-white shadow-lg shadow-[#D96C2C]/40 transition hover:from-[#BF5720] hover:to-[#a84614] focus-visible:ring-4 focus-visible:ring-[#D96C2C]/40 focus-visible:outline-none active:scale-95 sm:px-7 sm:text-base"
            >
              <span class="text-white">ค้นหา</span>
              <i class="mdi mdi-arrow-right text-lg text-white"></i>
            </button>
          </form>

          <!-- Quick links (real categories & districts) -->
          <div
            v-if="quickLinks.length"
            class="hero-rise hidden max-w-3xl flex-wrap items-center gap-2 sm:flex"
            style="animation-delay: 320ms"
          >
            <span class="mr-1 text-xs font-bold text-[#F7F0E6]/80 sm:text-sm">ลองสำรวจ:</span>
            <RouterLink
              v-for="link in quickLinks"
              :key="link.label"
              :to="link.to"
              class="rounded-full border border-white/30 bg-black/40 px-3.5 py-1.5 text-xs font-bold text-white backdrop-blur-md transition hover:border-[#F2A65A] hover:bg-white/20 focus-visible:ring-4 focus-visible:ring-white/30 focus-visible:outline-none sm:text-sm"
            >
              {{ link.label }}
            </RouterLink>
          </div>
        </div>

        <!-- RIGHT: latest story + live numbers -->
        <div class="hero-rise min-w-0 space-y-4 lg:col-span-5" style="animation-delay: 200ms">
          <RouterLink
            v-if="recommendedContents[0]"
            :to="`/contents/${recommendedContents[0].contentId}`"
            class="group hidden rounded-[1.75rem] border border-white/20 bg-black/45 p-3 shadow-2xl shadow-black/30 backdrop-blur-xl transition duration-300 hover:-translate-y-1 hover:bg-white/15 focus-visible:ring-4 focus-visible:ring-white/30 focus-visible:outline-none lg:block"
          >
            <div
              class="hero-story-img relative aspect-16/10 overflow-hidden rounded-2xl bg-[#171412]"
            >
              <img
                v-if="youtubeThumbnail(recommendedContents[0].youtubeUrl)"
                :src="youtubeThumbnail(recommendedContents[0].youtubeUrl, 'maxresdefault')"
                :alt="recommendedContents[0].title"
                class="h-full w-full object-cover transition-transform duration-700 group-hover:scale-105"
                @error="fallbackThumb($event, recommendedContents[0].youtubeUrl)"
              />
              <div
                v-else
                class="flex h-full w-full items-center justify-center bg-gradient-to-br from-[#D96C2C] via-[#BF5720] to-[#332820]"
              >
                <i class="mdi mdi-compass-rose text-6xl opacity-40"></i>
              </div>
              <div
                class="absolute inset-0 bg-gradient-to-t from-black/60 via-transparent to-black/10"
              ></div>
              <span
                class="absolute top-3 left-3 flex items-center gap-1.5 rounded-full bg-[#D96C2C]/95 px-3 py-1 text-xs font-black text-white shadow-md"
              >
                <i class="mdi mdi-star-four-points text-xs"></i>
                เรื่องราวเด่น
              </span>
              <span
                v-if="recommendedContents[0].youtubeUrl"
                class="absolute right-3 bottom-3 flex h-10 w-10 items-center justify-center rounded-full border border-white/30 bg-black/50 backdrop-blur-md transition group-hover:scale-110"
              >
                <i class="mdi mdi-play ml-0.5 text-xl text-[#F2A65A]"></i>
              </span>
            </div>
            <div class="space-y-1 px-2 pt-3 pb-1">
              <h2 class="line-clamp-2 text-base leading-snug font-black text-white sm:text-lg">
                {{ recommendedContents[0].title }}
              </h2>
              <p class="flex items-center gap-1.5 text-xs font-semibold text-[#F7F0E6]/80">
                <i class="mdi mdi-map-marker text-[#F2A65A]"></i>
                {{ recommendedContents[0].districtName || 'กาญจนบุรี' }}
                <span class="ml-auto flex items-center gap-1 font-black text-[#F7C07A]">
                  อ่านเรื่องราว <i class="mdi mdi-arrow-right"></i>
                </span>
              </p>
            </div>
          </RouterLink>

          <div v-if="routeStats.length" class="grid grid-cols-3 gap-3">
            <div
              v-for="stat in routeStats"
              :key="stat.label"
              class="rounded-2xl border border-white/20 bg-black/45 px-3 py-4 text-center shadow-lg shadow-black/20 backdrop-blur-xl"
            >
              <i :class="`mdi ${stat.icon}`" class="text-2xl text-[#F2A65A]"></i>
              <span
                class="mt-1 block font-display text-2xl leading-none font-black text-white sm:text-3xl"
                ><AnimatedNumber :value="stat.value" /></span
              >
              <span
                class="mt-1.5 block text-[11px] leading-tight font-bold text-[#F7F0E6] sm:text-xs"
                >{{ stat.label }}</span
              >
            </div>
          </div>
        </div>
      </div>

      <!-- Fade into the page background + scroll cue -->
      <div
        class="pointer-events-none absolute inset-x-0 bottom-0 -z-10 h-32 bg-gradient-to-t from-[#171412]/70 to-transparent"
      ></div>
      <button
        type="button"
        class="absolute bottom-6 left-1/2 flex -translate-x-1/2 cursor-pointer flex-col items-center gap-1 text-white/90 transition hover:text-white focus-visible:ring-4 focus-visible:ring-white/30 focus-visible:outline-none sm:bottom-8"
        aria-label="เลื่อนลงเพื่อดูรายละเอียด"
        @click="scrollToContent"
      >
        <span
          class="rounded-full border border-white/30 bg-black/35 px-4 py-1.5 text-xs font-black tracking-wide backdrop-blur-md sm:text-sm"
        >
          เลื่อนลงเพื่อดูรายละเอียด
        </span>
        <i
          class="mdi mdi-chevron-double-down animate-bounce text-2xl leading-none text-[#F2A65A]"
        ></i>
      </button>
    </section>

    <!-- 2. CATEGORIES — TRANSIT LINE STRIP -->
    <section
      v-if="categories.length"
      class="relative z-20 mx-auto w-full max-w-[1800px] px-4 sm:px-6 lg:px-10 mt-2"
    >
      <div
        class="rounded-2xl bg-[#FFF9F2] border-2 border-[#E8D9C9] shadow-xl px-5 sm:px-7 py-3.5 flex items-center gap-5 overflow-x-auto scrollbar-none"
      >
        <span class="text-xs font-black text-[#786B62] shrink-0 flex items-center gap-1">
          <i class="mdi mdi-routes text-[#D96C2C] text-sm"></i>
          สายการท่องเที่ยว
        </span>
        <RouterLink
          v-for="(cat, i) in categories"
          :key="cat.contentCategoryId"
          :to="{ path: '/contents', query: { categoryId: String(cat.contentCategoryId) } }"
          class="shrink-0 flex items-center gap-2 group cursor-pointer"
        >
          <span
            class="h-2.5 w-2.5 rounded-full shrink-0"
            :style="{ background: lineColors[i % lineColors.length] }"
          ></span>
          <span
            class="text-xs sm:text-sm font-black text-[#332820] group-hover:text-[#D96C2C] whitespace-nowrap transition"
          >
            {{ cat.categoryName }}
          </span>
        </RouterLink>
        <RouterLink
          to="/contents"
          class="ml-auto shrink-0 text-xs font-black text-[#D96C2C] hover:underline whitespace-nowrap flex items-center gap-0.5"
        >
          <span>ดูทั้งหมด</span>
          <i class="mdi mdi-chevron-right text-base"></i>
        </RouterLink>
      </div>
    </section>

    <!-- 3. FROM STORY TO ECONOMY -->
    <section v-reveal class="relative mx-auto w-full max-w-[1800px] px-4 py-14 sm:px-6 sm:py-20 lg:px-10">
      <div class="mx-auto max-w-3xl space-y-3 text-center">
        <p class="text-[11px] font-black tracking-[0.2em] text-[#D96C2C] sm:text-xs">
          เศรษฐกิจสร้างสรรค์
        </p>
        <h2 class="font-display text-3xl leading-tight font-black text-[#332820] sm:text-4xl">
          จากเรื่องเล่าของชุมชน
          <span class="inline-block text-[#D96C2C]">สู่โอกาสสร้างรายได้</span>
        </h2>
        <p class="text-sm leading-relaxed font-medium text-[#786B62] sm:text-base">
          เรื่องราวทุกเรื่องบนแพลตฟอร์มเชื่อมถึงร้านค้าและสินค้าท้องถิ่น
          ให้คอนเทนต์ของชุมชนเป็นประตูสู่การอุดหนุนและรายได้ที่ยั่งยืน
        </p>
      </div>

      <ol class="relative mt-10 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
        <div
          class="pointer-events-none absolute top-[3.1rem] right-[12.5%] left-[12.5%] hidden border-t-2 border-dashed border-[#D96C2C]/30 lg:block"
        ></div>
        <li
          v-for="(step, i) in flowSteps"
          :key="step.title"
          v-reveal
          :style="{ animationDelay: `${i * 90}ms` }"
          class="relative z-10 rounded-[1.75rem] border-2 border-[#E8D9C9] bg-white p-6 text-center shadow-md shadow-[#D96C2C]/5 transition duration-300 hover:-translate-y-1.5 hover:border-[#D96C2C] hover:shadow-2xl hover:shadow-[#D96C2C]/10"
        >
          <span
            class="absolute -top-3 left-1/2 flex h-7 w-7 -translate-x-1/2 items-center justify-center rounded-full bg-[#332820] text-xs font-black text-white ring-4 ring-[#F7F0E6]"
          >
            {{ i + 1 }}
          </span>
          <div
            class="mx-auto flex h-16 w-16 items-center justify-center rounded-2xl bg-gradient-to-br from-[#D96C2C] to-[#BF5720] text-3xl text-white shadow-lg shadow-[#D96C2C]/30"
          >
            <i :class="`mdi ${step.icon}`" class="text-white"></i>
          </div>
          <h3 class="mt-4 font-display text-lg font-black text-[#332820]">{{ step.title }}</h3>
          <p class="mt-1.5 text-xs leading-relaxed font-medium text-[#786B62] sm:text-sm">
            {{ step.desc }}
          </p>
        </li>
      </ol>

      <div class="mt-10 flex flex-wrap items-center justify-center gap-3">
        <RouterLink
          to="/create"
          class="inline-flex items-center gap-2 rounded-full bg-gradient-to-r from-[#D96C2C] to-[#BF5720] px-6 py-3 text-sm font-black text-white shadow-lg shadow-[#D96C2C]/30 transition hover:from-[#BF5720] hover:to-[#a84614] focus-visible:ring-4 focus-visible:ring-[#D96C2C]/30 focus-visible:outline-none active:scale-95"
        >
          <i class="mdi mdi-plus-circle text-lg text-white"></i>
          <span class="text-white">เริ่มลงคอนเทนต์</span>
        </RouterLink>
        <RouterLink
          to="/shops"
          class="inline-flex items-center gap-2 rounded-full border-2 border-[#E8D9C9] bg-white px-6 py-3 text-sm font-black text-[#332820] transition hover:border-[#D96C2C] hover:text-[#D96C2C] focus-visible:ring-4 focus-visible:ring-[#D96C2C]/20 focus-visible:outline-none active:scale-95"
        >
          <i class="mdi mdi-storefront-outline text-lg text-[#D96C2C]"></i>
          <span>ดูร้านค้าชุมชน</span>
        </RouterLink>
      </div>
    </section>

    <div class="mx-auto w-full max-w-[1800px] px-4 sm:px-6 lg:px-10 py-10 space-y-12 sm:space-y-16">
      <!-- 4. ACTIVE DEALS -->
      <section v-if="visibleDealProducts.length">
        <div v-reveal class="flex items-end justify-between gap-4 pb-5">
          <div>
            <p
              class="inline-flex items-center gap-2 text-[11px] font-black tracking-[0.2em] text-rose-600 sm:text-xs"
            >
              <span class="relative flex h-2 w-2">
                <span
                  class="absolute inline-flex h-full w-full animate-ping rounded-full bg-rose-500 opacity-60"
                ></span>
                <span class="relative inline-flex h-2 w-2 rounded-full bg-rose-600"></span>
              </span>
              ข้อเสนอพิเศษ
            </p>
            <h2 class="mt-1 font-display text-2xl leading-[1.35] font-black text-[#332820] sm:text-3xl">
              โปรโมชันพิเศษ
            </h2>
            <p class="text-xs font-semibold text-[#786B62] sm:text-sm">
              ราคาพิเศษมีเวลาจำกัด แต่ละสินค้ามีเวลาสิ้นสุดไม่เท่ากัน
            </p>
          </div>
          <RouterLink
            to="/shops"
            class="group flex shrink-0 items-center gap-1.5 rounded-full border border-[#E8D9C9] bg-white px-4 py-2 text-xs font-black text-[#332820] transition hover:border-rose-400 hover:text-rose-700 focus-visible:ring-4 focus-visible:ring-rose-200 focus-visible:outline-none sm:text-sm"
          >
            <span>ดูทั้งหมด</span>
            <i class="mdi mdi-arrow-right text-base transition group-hover:translate-x-0.5"></i>
          </RouterLink>
        </div>

        <div class="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
          <RouterLink
            v-for="(product, index) in visibleDealProducts"
            :key="product.productId"
            v-reveal
            :style="{ animationDelay: `${(index % 5) * 70}ms` }"
            :to="`/products/${product.productId}`"
            class="group flex flex-col overflow-hidden rounded-3xl border border-[#E8D9C9] bg-white transition duration-300 hover:-translate-y-1 hover:border-rose-300 hover:shadow-xl hover:shadow-[#332820]/10 focus-visible:ring-4 focus-visible:ring-rose-200 focus-visible:outline-none"
          >
            <div class="relative aspect-4/3 overflow-hidden bg-[#F7F0E6]">
              <img
                v-if="getProductCover(product, index)"
                :src="getProductCover(product, index)"
                @error="failedCovers[product.productId] = true"
                :alt="product.productName"
                class="h-full w-full object-cover transition duration-500 group-hover:scale-105"
              />
              <div v-else class="flex h-full w-full items-center justify-center text-[#E8D9C9]">
                <i class="mdi mdi-image-outline text-4xl"></i>
              </div>
              <span
                class="absolute top-3 left-3 rounded-full bg-white/95 px-2.5 py-1 text-[10px] font-black text-rose-700 shadow-sm"
              >
                <i class="mdi mdi-lightning-bolt mr-0.5"></i>{{ dealLabel(product) }}
              </span>
              <span
                class="absolute top-3 right-3 rounded-full bg-rose-600 px-2.5 py-1 text-[11px] font-black text-white shadow-md"
              >
                {{ discountLabel(product) }}
              </span>
            </div>

            <div class="flex flex-1 flex-col p-4">
              <p class="line-clamp-1 text-[11px] font-bold text-[#786B62]">
                {{ product.shopName || 'ร้านค้าชุมชน' }}
              </p>
              <h3
                class="mt-1 line-clamp-2 text-sm leading-snug font-black text-[#332820] transition group-hover:text-rose-700 sm:text-base"
              >
                {{ product.productName }}
              </h3>

              <div class="mt-3 flex flex-wrap items-baseline gap-x-2">
                <span class="text-lg font-black text-rose-600 sm:text-xl">
                  ฿{{
                    Number(dealPrice(product)).toLocaleString('th-TH', { maximumFractionDigits: 2 })
                  }}
                </span>
                <span class="text-xs font-semibold text-[#A8988C] line-through">
                  ฿{{ Number(product.price).toLocaleString('th-TH') }}
                </span>
              </div>

              <div
                class="mt-auto flex items-center justify-between gap-2 border-t border-dashed border-[#E8D9C9] pt-3 text-[11px] font-bold"
              >
                <span class="flex items-center gap-1 text-rose-700">
                  <i class="mdi mdi-clock-outline text-sm"></i>
                  <span class="tabular-nums">{{
                    remainingDealTime(product.activeDeal!.endsAt)
                  }}</span>
                </span>
                <span class="text-[#786B62]">เหลือ {{ product.activeDeal?.availableQuantity }} สิทธิ์</span>
              </div>
            </div>
          </RouterLink>
        </div>
      </section>

      <!-- 5. RECOMMENDED STORIES -->
      <section v-if="recommendedContents.length || loading">
        <div v-reveal class="mb-6 flex items-end justify-between gap-4">
          <div>
            <p
              class="inline-flex items-center gap-2 text-[11px] font-black tracking-[0.2em] text-[#D96C2C] sm:text-xs"
            >
              <i class="mdi mdi-book-open-page-variant-outline text-base"></i>
              คอนเทนต์ท้องถิ่น
            </p>
            <h2 class="mt-1 font-display text-2xl leading-[1.35] font-black text-[#332820] sm:text-3xl">
              เรื่องราวและคอนเทนต์แนะนำ
            </h2>
            <p class="text-xs font-semibold text-[#786B62] sm:text-sm">
              รวมเรื่องน่าสนใจ สถานที่ท่องเที่ยว และภูมิปัญญากาญจนบุรี
            </p>
          </div>
          <RouterLink
            to="/contents"
            class="group flex shrink-0 items-center gap-1.5 rounded-full bg-[#332820] px-4 py-2 text-xs font-black text-white transition hover:bg-[#D96C2C] focus-visible:ring-4 focus-visible:ring-[#D96C2C]/30 focus-visible:outline-none sm:text-sm"
          >
            <span class="text-white">ดูทั้งหมด</span>
            <i class="mdi mdi-arrow-right text-base text-white transition group-hover:translate-x-0.5"></i>
          </RouterLink>
        </div>

        <div v-if="loading" class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-5 sm:gap-6">
          <div
            v-for="i in 6"
            :key="i"
            class="animate-pulse bg-[#FFF9F2] rounded-[1.75rem] h-64 border-2 border-[#E8D9C9]"
          ></div>
        </div>

        <div
          v-else-if="recommendedContents.length"
          class="grid grid-cols-1 gap-5 sm:grid-cols-2 sm:gap-6 lg:grid-cols-4"
        >
          <RouterLink
            v-for="(item, sIdx) in recommendedContents.slice(0, 7)"
            :key="item.contentId"
            v-reveal
            :class="[
              sIdx >= 3 ? 'max-lg:hidden' : '',
              sIdx === 0 ? 'min-h-[340px] sm:col-span-2' : '',
            ]"
            :style="{ animationDelay: `${(sIdx % 4) * 90}ms` }"
            :to="`/contents/${item.contentId}`"
            class="group relative flex flex-col justify-between overflow-hidden rounded-3xl border border-[#E8D9C9] bg-white transition duration-300 hover:-translate-y-1 hover:border-[#D96C2C]/60 hover:shadow-xl hover:shadow-[#332820]/10 focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25 focus-visible:outline-none"
          >
            <!-- Featured story: full-bleed photo with overlaid text -->
            <template v-if="sIdx === 0">
              <img
                v-if="youtubeThumbnail(item.youtubeUrl)"
                :src="youtubeThumbnail(item.youtubeUrl, 'maxresdefault')"
                :alt="item.title"
                class="absolute inset-0 h-full w-full object-cover transition-transform duration-700 group-hover:scale-105"
                @error="fallbackThumb($event, item.youtubeUrl)"
              />
              <div
                v-else
                class="absolute inset-0 flex items-center justify-center bg-gradient-to-br from-[#D96C2C] via-[#BF5720] to-[#171412] text-white"
              >
                <i class="mdi mdi-compass-rose text-8xl opacity-30"></i>
              </div>
              <div
                class="absolute inset-0 bg-gradient-to-t from-[#171412]/95 via-[#171412]/55 to-[#171412]/5"
              ></div>

              <span
                v-if="item.youtubeUrl"
                class="absolute top-1/2 left-1/2 flex h-16 w-16 -translate-x-1/2 -translate-y-1/2 items-center justify-center rounded-full border border-white/40 bg-white/20 text-3xl text-white shadow-xl backdrop-blur-md transition duration-300 group-hover:scale-110 group-hover:bg-[#D96C2C]"
              >
                <i class="mdi mdi-play ml-0.5 text-white"></i>
              </span>

              <div class="relative flex items-start justify-between gap-2 p-5">
                <span
                  class="flex items-center gap-1.5 rounded-full bg-[#D96C2C]/95 px-3.5 py-1 text-xs font-black text-white shadow-md"
                >
                  <i class="mdi mdi-star-four-points text-xs"></i>
                  เรื่องราวเด่น
                </span>
                <span
                  v-if="item.contentCategoryName"
                  class="rounded-full border border-white/30 bg-black/30 px-3 py-1 text-xs font-bold text-white backdrop-blur-md"
                >
                  {{ item.contentCategoryName }}
                </span>
              </div>

              <div class="relative space-y-2 p-5 sm:p-7">
                <h3
                  class="font-display line-clamp-2 text-xl leading-snug font-black text-white drop-shadow-lg sm:text-2xl lg:text-[1.65rem]"
                >
                  {{ item.title }}
                </h3>
                <p
                  v-if="item.summary"
                  class="line-clamp-2 max-w-2xl text-sm leading-relaxed font-medium text-[#F7F0E6]/90 sm:text-base lg:hidden"
                >
                  {{ item.summary }}
                </p>
                <div class="flex items-center justify-between pt-1 text-sm font-bold text-white">
                  <span class="flex items-center gap-1.5">
                    <i class="mdi mdi-map-marker text-[#F2A65A]"></i>
                    {{ item.districtName || 'กาญจนบุรี' }}
                  </span>
                  <span
                    class="flex items-center gap-1.5 rounded-full bg-white px-4 py-2 text-sm font-black text-[#332820] shadow-lg transition group-hover:bg-[#D96C2C] group-hover:text-white"
                  >
                    อ่านเรื่องราว <i class="mdi mdi-arrow-right text-base"></i>
                  </span>
                </div>
              </div>
            </template>

            <!-- Regular story card -->
            <template v-else>
              <div>
                <div class="relative aspect-16/10 overflow-hidden bg-[#171412]">
                  <img
                    v-if="youtubeThumbnail(item.youtubeUrl)"
                    :src="youtubeThumbnail(item.youtubeUrl, 'maxresdefault')"
                    :alt="item.title"
                    class="h-full w-full object-cover transition duration-500 group-hover:scale-105"
                    @error="fallbackThumb($event, item.youtubeUrl)"
                  />
                  <div
                    v-else
                    class="flex h-full w-full items-center justify-center bg-gradient-to-br from-[#D96C2C] via-[#BF5720] to-[#171412] text-white"
                  >
                    <i class="mdi mdi-compass-rose text-5xl opacity-40"></i>
                  </div>
                  <div
                    class="pointer-events-none absolute inset-0 bg-gradient-to-t from-black/45 via-transparent to-transparent"
                  ></div>
                  <span
                    class="absolute top-3 left-3 max-w-[80%] truncate rounded-full bg-white/95 px-3 py-1 text-xs font-black text-[#BF5720] shadow-sm"
                  >
                    {{ item.contentCategoryName || 'เรื่องราวชุมชน' }}
                  </span>
                  <span
                    v-if="item.youtubeUrl"
                    class="absolute right-3 bottom-3 flex h-10 w-10 items-center justify-center rounded-full bg-[#D96C2C] text-xl text-white shadow-lg transition duration-300 group-hover:scale-110"
                  >
                    <i class="mdi mdi-play ml-0.5 text-white"></i>
                  </span>
                </div>

                <div class="space-y-2 p-4 sm:p-5">
                  <h3
                    class="font-display line-clamp-2 text-base leading-snug font-black text-[#332820] transition group-hover:text-[#D96C2C] sm:text-lg"
                  >
                    {{ item.title }}
                  </h3>
                </div>
              </div>

              <div
                class="flex items-center justify-between gap-2 px-4 pt-1 pb-4 text-xs font-bold text-[#786B62] sm:px-5 sm:text-sm"
              >
                <span class="flex min-w-0 items-center gap-1 text-[#332820]">
                  <i class="mdi mdi-map-marker text-base text-[#D96C2C]"></i>
                  <span class="truncate">{{ item.districtName || 'กาญจนบุรี' }}</span>
                </span>
                <span
                  class="flex shrink-0 items-center gap-1 rounded-full bg-[#FFF1E8] px-3 py-1.5 font-black text-[#BF5720] transition group-hover:bg-[#D96C2C] group-hover:text-white"
                >
                  <span>อ่านต่อ</span>
                  <i class="mdi mdi-arrow-right text-base"></i>
                </span>
              </div>
            </template>
          </RouterLink>
        </div>
      </section>

      <!-- 5. DISTRICTS — HORIZONTAL ROUTE MAP -->
      <section
        v-if="districts.length"
        class="relative isolate overflow-hidden rounded-[2rem] bg-[#211a16] px-5 py-8 text-white sm:px-8 sm:py-10 lg:px-12"
      >
        <!-- decorative glows + dot grid -->
        <div
          class="pointer-events-none absolute -top-28 -right-20 -z-10 h-80 w-80 rounded-full bg-[#D96C2C]/35 blur-3xl"
        ></div>
        <div
          class="pointer-events-none absolute -bottom-28 -left-16 -z-10 h-72 w-72 rounded-full bg-[#F2A65A]/20 blur-3xl"
        ></div>
        <div
          class="pointer-events-none absolute inset-0 -z-10 opacity-[0.07]"
          style="background-image: radial-gradient(#fff 1px, transparent 1px); background-size: 22px 22px"
        ></div>

        <div v-reveal class="mb-7 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <p
              class="inline-flex items-center gap-2 text-[11px] font-black tracking-[0.2em] text-[#F2A65A] sm:text-xs"
            >
              <i class="mdi mdi-map-marker-radius-outline text-base"></i>
              สำรวจพื้นที่
            </p>
            <h2
              class="mt-1 font-display text-2xl leading-[1.35] font-black text-white sm:text-3xl"
            >
              สำรวจตามอำเภอและพื้นที่
            </h2>
            <p class="text-xs font-semibold text-[#F7F0E6]/80 sm:text-sm">
              เลือกท่องเที่ยวและค้นหาเรื่องราวรายอำเภอทั่วกาญจนบุรี
            </p>
          </div>
          <RouterLink
            to="/contents"
            class="group flex shrink-0 items-center gap-1.5 self-start rounded-full bg-white px-4 py-2 text-xs font-black text-[#332820] transition hover:bg-[#F2A65A] focus-visible:ring-4 focus-visible:ring-white/40 focus-visible:outline-none sm:self-auto sm:text-sm"
          >
            <span>ดูพื้นที่ทั้งหมด</span>
            <i class="mdi mdi-arrow-right text-base transition group-hover:translate-x-0.5"></i>
          </RouterLink>
        </div>

        <div class="grid grid-cols-2 gap-3 sm:grid-cols-3 sm:gap-4 lg:grid-cols-4 xl:grid-cols-6">
          <RouterLink
            v-for="(dist, dIdx) in districts"
            :key="dist.districtId"
            v-reveal
            :style="{ animationDelay: `ms` }"
            :to="{ path: '/contents', query: { districtId: String(dist.districtId) } }"
            class="group relative flex min-h-[104px] flex-col justify-between overflow-hidden rounded-2xl border border-white/15 bg-white/[0.07] p-4 backdrop-blur-sm transition duration-300 hover:-translate-y-1 hover:border-[#F2A65A] hover:bg-gradient-to-br hover:from-[#D96C2C] hover:to-[#BF5720] hover:shadow-xl hover:shadow-black/30 focus-visible:ring-4 focus-visible:ring-[#F2A65A]/50 focus-visible:outline-none active:scale-95"
          >
            <span
              class="pointer-events-none absolute -right-1 -bottom-4 font-display text-7xl leading-none font-black text-white/[0.07] transition group-hover:text-white/20"
              aria-hidden="true"
              >{{ String(dIdx + 1).padStart(2, '0') }}</span
            >
            <span
              class="flex h-9 w-9 items-center justify-center rounded-xl bg-[#D96C2C]/90 text-lg text-white transition group-hover:bg-white group-hover:text-[#D96C2C]"
            >
              <i class="mdi mdi-map-marker"></i>
            </span>
            <span class="relative mt-3 flex items-end justify-between gap-2">
              <span class="text-sm leading-snug font-black text-white sm:text-base">
                อ.{{ dist.districtName }}
              </span>
              <i
                class="mdi mdi-arrow-top-right text-lg text-white/50 transition group-hover:translate-x-0.5 group-hover:-translate-y-0.5 group-hover:text-white"
              ></i>
            </span>
          </RouterLink>
        </div>
      </section>

      <!-- 6. SHOPS -->
      <section v-if="shops.length || loading">
        <div v-reveal class="mb-6 flex items-end justify-between gap-4">
          <div>
            <p
              class="inline-flex items-center gap-2 text-[11px] font-black tracking-[0.2em] text-[#D96C2C] sm:text-xs"
            >
              <i class="mdi mdi-store-outline text-base"></i>
              ร้านค้าชุมชน
            </p>
            <h2 class="mt-1 font-display text-2xl leading-[1.35] font-black text-[#332820] sm:text-3xl">
              ร้านค้าชุมชนแนะนำ
            </h2>
            <p class="text-xs font-semibold text-[#786B62] sm:text-sm">
              อุดหนุนสินค้าท้องถิ่น ส่งตรงจากชาวบ้านและเกษตรกรกาญจนบุรี
            </p>
          </div>
          <RouterLink
            to="/shops"
            class="group flex shrink-0 items-center gap-1.5 rounded-full bg-[#332820] px-4 py-2 text-xs font-black text-white transition hover:bg-[#D96C2C] focus-visible:ring-4 focus-visible:ring-[#D96C2C]/30 focus-visible:outline-none sm:text-sm"
          >
            <span class="text-white">ดูทั้งหมด</span>
            <i class="mdi mdi-arrow-right text-base text-white transition group-hover:translate-x-0.5"></i>
          </RouterLink>
        </div>

        <div v-if="loading" class="grid grid-cols-1 gap-5 sm:grid-cols-2 sm:gap-6 lg:grid-cols-4">
          <div
            v-for="i in 4"
            :key="i"
            class="h-72 animate-pulse rounded-3xl border border-[#E8D9C9] bg-[#FFF9F2]"
          ></div>
        </div>

        <div
          v-else-if="shops.length"
          class="grid grid-cols-1 gap-5 sm:grid-cols-2 sm:gap-6 lg:grid-cols-4"
        >
          <RouterLink
            v-for="(shop, idx) in shops"
            :key="shop.shopId"
            v-reveal
            :class="idx >= 4 ? 'max-sm:hidden' : ''"
            :style="{ animationDelay: `${(idx % 4) * 80}ms` }"
            :to="`/shops/${shop.shopId}`"
            class="group flex flex-col overflow-hidden rounded-3xl border border-[#E8D9C9] bg-white transition duration-300 hover:-translate-y-1 hover:border-[#D96C2C]/60 hover:shadow-xl hover:shadow-[#332820]/10 focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25 focus-visible:outline-none"
          >
            <div class="relative aspect-16/10 overflow-hidden bg-[#171412]">
              <img
                v-if="getShopCover(shop, idx)"
                :src="getShopCover(shop, idx)"
                @error="failedCovers[shop.shopId] = true"
                :alt="shop.shopName"
                class="h-full w-full object-cover object-center transition duration-500 group-hover:scale-105"
              />
              <div
                v-else
                class="flex h-full w-full items-center justify-center bg-gradient-to-br from-[#D96C2C] via-[#BF5720] to-[#332820] text-white"
              >
                <i class="mdi mdi-storefront-outline text-5xl opacity-40"></i>
              </div>
              <div
                class="pointer-events-none absolute inset-0 bg-gradient-to-t from-black/50 via-transparent to-transparent"
              ></div>
              <span
                v-if="shop.status === 'Active'"
                class="absolute top-3 left-3 flex items-center gap-1 rounded-full bg-white/95 px-2.5 py-1 text-[11px] font-black text-emerald-700 shadow-sm"
              >
                <i class="mdi mdi-check-decagram text-sm"></i>
                ผ่านการรับรอง
              </span>
            </div>

            <div class="relative flex flex-1 flex-col px-4 pt-0 pb-4 sm:px-5">
              <!-- storefront badge overlapping the photo -->
              <span
                class="-mt-6 flex h-12 w-12 items-center justify-center rounded-2xl border-4 border-white bg-gradient-to-br from-[#D96C2C] to-[#BF5720] text-2xl text-white shadow-lg"
              >
                <i class="mdi mdi-storefront-outline text-white"></i>
              </span>
              <h3
                class="mt-2 line-clamp-1 font-display text-base font-black text-[#332820] transition group-hover:text-[#D96C2C] sm:text-lg"
              >
                {{ shop.shopName }}
              </h3>
              <p class="mt-0.5 flex items-center gap-1 text-xs font-semibold text-[#786B62] sm:text-sm">
                <i class="mdi mdi-map-marker text-base text-[#D96C2C]"></i>
                <span class="truncate">{{
                  shop.districtName ? `อ.${shop.districtName} ` : ''
                }}จ.กาญจนบุรี</span>
              </p>

              <span
                class="mt-4 flex w-full items-center justify-center gap-1.5 rounded-xl bg-[#FFF1E8] py-2.5 text-xs font-black text-[#BF5720] transition group-hover:bg-[#D96C2C] group-hover:text-white sm:text-sm"
              >
                <span>เข้าชมร้านค้า</span>
                <i class="mdi mdi-arrow-right text-base transition group-hover:translate-x-0.5"></i>
              </span>
            </div>
          </RouterLink>
        </div>
      </section>

      <!-- 7. PRODUCTS — LUGGAGE-TAG PRICE BADGE -->
      <section v-if="products.length || loading">
        <div v-reveal class="mb-6 flex items-end justify-between gap-4">
          <div>
            <p
              class="inline-flex items-center gap-2 text-[11px] font-black tracking-[0.2em] text-[#D96C2C] sm:text-xs"
            >
              <i class="mdi mdi-package-variant-closed text-base"></i>
              สินค้าท้องถิ่น
            </p>
            <h2 class="mt-1 font-display text-2xl leading-[1.35] font-black text-[#332820] sm:text-3xl">
              สินค้าขึ้นชื่อแนะนำ
            </h2>
            <p class="text-xs font-semibold text-[#786B62] sm:text-sm">
              ของฝาก หัตถกรรม ผลิตภัณฑ์เกษตร และของดีเมืองกาญจน์
            </p>
          </div>
          <RouterLink
            to="/shops"
            class="group flex shrink-0 items-center gap-1.5 rounded-full bg-[#332820] px-4 py-2 text-xs font-black text-white transition hover:bg-[#D96C2C] focus-visible:ring-4 focus-visible:ring-[#D96C2C]/30 focus-visible:outline-none sm:text-sm"
          >
            <span class="text-white">ดูทั้งหมด</span>
            <i class="mdi mdi-arrow-right text-base text-white transition group-hover:translate-x-0.5"></i>
          </RouterLink>
        </div>

        <div v-if="loading" class="grid grid-cols-2 gap-4 sm:gap-5 md:grid-cols-4">
          <div
            v-for="i in 4"
            :key="i"
            class="h-72 animate-pulse rounded-3xl border border-[#E8D9C9] bg-[#FFF9F2]"
          ></div>
        </div>

        <div v-else-if="products.length" class="grid grid-cols-2 gap-4 sm:gap-5 md:grid-cols-4">
          <RouterLink
            v-for="(prod, idx) in products"
            :key="prod.productId"
            v-reveal
            :class="idx >= 4 ? 'max-sm:hidden' : ''"
            :style="{ animationDelay: `${(idx % 4) * 80}ms` }"
            :to="`/products/${prod.productId}`"
            class="group flex flex-col overflow-hidden rounded-3xl border border-[#E8D9C9] bg-white transition duration-300 hover:-translate-y-1 hover:border-[#D96C2C]/60 hover:shadow-xl hover:shadow-[#332820]/10 focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25 focus-visible:outline-none"
          >
            <div class="relative aspect-square overflow-hidden bg-[#F7F0E6] sm:aspect-4/3">
              <img
                v-if="getProductCover(prod, idx)"
                :src="getProductCover(prod, idx)"
                @error="failedCovers[prod.productId] = true"
                :alt="prod.productName"
                class="h-full w-full object-cover object-center transition duration-500 group-hover:scale-105"
              />
              <div v-else class="flex h-full w-full items-center justify-center text-[#E8D9C9]">
                <i class="mdi mdi-image-outline text-4xl"></i>
              </div>
              <span
                v-if="prod.activeDeal"
                class="absolute top-3 left-3 flex items-center gap-1 rounded-full bg-rose-600 px-2.5 py-1 text-[11px] font-black text-white shadow-md"
              >
                <i class="mdi mdi-lightning-bolt"></i>{{ dealLabel(prod) }}
              </span>
              <span
                v-if="prod.quantity > 0 && prod.quantity <= 5"
                class="absolute bottom-3 left-3 rounded-full bg-black/60 px-2.5 py-1 text-[11px] font-bold text-white backdrop-blur-md"
              >
                เหลือ {{ prod.quantity }} ชิ้น
              </span>
              <span
                v-if="prod.quantity <= 0"
                class="absolute inset-0 flex items-center justify-center bg-[#332820]/60 text-sm font-black text-white"
              >
                สินค้าหมด
              </span>
              <!-- quick add button -->
              <span
                v-if="prod.quantity > 0"
                class="absolute right-3 bottom-3 flex h-10 w-10 translate-y-2 items-center justify-center rounded-full bg-[#D96C2C] text-xl text-white opacity-0 shadow-lg transition duration-300 group-hover:translate-y-0 group-hover:opacity-100 max-sm:translate-y-0 max-sm:opacity-100"
              >
                <i class="mdi mdi-cart-plus text-white"></i>
              </span>
            </div>

            <div class="flex flex-1 flex-col p-3.5 sm:p-4">
              <p v-if="prod.shopName" class="line-clamp-1 text-[11px] font-bold text-[#786B62]">
                {{ prod.shopName }}
              </p>
              <h3
                class="mt-0.5 line-clamp-2 font-display text-sm leading-snug font-black text-[#332820] transition group-hover:text-[#D96C2C] sm:text-base"
              >
                {{ prod.productName }}
              </h3>

              <div class="mt-auto flex flex-wrap items-baseline gap-x-2 pt-3">
                <span class="text-lg font-black text-[#D96C2C] sm:text-xl">
                  ฿{{
                    Number(dealPrice(prod)).toLocaleString('th-TH', { maximumFractionDigits: 2 })
                  }}
                </span>
                <span
                  v-if="prod.activeDeal"
                  class="text-xs font-semibold text-[#A8988C] line-through"
                >
                  ฿{{ Number(prod.price).toLocaleString('th-TH') }}
                </span>
              </div>
            </div>
          </RouterLink>
        </div>
      </section>

      <!-- 8. CTA — TWO TICKET STUBS SIDE BY SIDE (WITH APPS BRAND COLORS) -->
      <section v-reveal
        class="relative rounded-[1.75rem] border-2 border-[#E8D9C9] overflow-hidden shadow-2xl"
      >
        <div class="grid sm:grid-cols-2">
          <div
            class="relative bg-gradient-to-br from-[#171412] to-[#2d1f14] text-white p-8 sm:p-10 space-y-4"
          >
            <span
              class="text-xs font-black text-[#F2A65A] bg-[#F2A65A]/10 px-3 py-1 rounded-full border border-[#F2A65A]/30 inline-block"
            >
              สำหรับผู้ประกอบการ
            </span>
            <h2 class="font-display text-2xl sm:text-3xl font-black leading-tight text-white">
              เริ่มต้นเปิดร้านค้าชุมชนของคุณ
            </h2>
            <p class="text-xs sm:text-sm text-[#E8D9C9] font-medium leading-relaxed">
              เพิ่มโอกาสสร้างรายได้สู่ชุมชน นำเสนอสินค้าโอทอปและสินค้าเกษตรให้คนทั่วประเทศเห็น
            </p>
            <RouterLink
              :to="shopCtaLink"
              class="inline-flex items-center gap-2 rounded-2xl bg-gradient-to-r from-[#D96C2C] to-[#BF5720] hover:from-[#BF5720] hover:to-[#a84614] text-white font-black text-xs sm:text-sm px-6 py-3 transition shadow-md border border-[#D96C2C] cursor-pointer"
            >
              <span>เริ่มต้นเปิดร้านค้า</span>
              <i class="mdi mdi-arrow-right text-base"></i>
            </RouterLink>
          </div>

          <!-- Perforation between the two stubs -->
          <div
            class="hidden sm:block absolute inset-y-4 left-1/2 border-l-2 border-dashed border-white/20 z-10"
          ></div>
          <span
            class="hidden sm:block absolute -top-3 left-1/2 -translate-x-1/2 h-6 w-6 rounded-full bg-[#F7F0E6] border-2 border-[#E8D9C9] z-20"
          ></span>
          <span
            class="hidden sm:block absolute -bottom-3 left-1/2 -translate-x-1/2 h-6 w-6 rounded-full bg-[#F7F0E6] border-2 border-[#E8D9C9] z-20"
          ></span>

          <div
            class="relative bg-gradient-to-br from-[#2C221E] to-[#171412] text-white p-8 sm:p-10 space-y-4"
          >
            <span
              class="text-xs font-black text-[#F2A65A] bg-[#F2A65A]/10 px-3 py-1 rounded-full border border-[#F2A65A]/30 inline-block"
            >
              สำหรับนักเล่าเรื่อง
            </span>
            <h2 class="font-display text-2xl sm:text-3xl font-black leading-tight text-white">
              เปลี่ยนเรื่องเล่าของคุณให้เป็นโอกาส
            </h2>
            <p class="text-xs sm:text-sm text-[#E8D9C9] font-medium leading-relaxed">
              ถ่ายทอดภูมิปัญญาท้องถิ่นและวิถีชุมชน เพื่อส่งต่อคุณค่าให้ผู้คนทั่วประเทศ
            </p>
            <RouterLink
              to="/create"
              class="inline-flex items-center gap-2 rounded-2xl border border-white/30 bg-white/10 hover:bg-white/20 text-white font-black text-xs sm:text-sm px-6 py-3 transition shadow-md cursor-pointer"
            >
              <span>แบ่งปันเรื่องราว</span>
              <i class="mdi mdi-arrow-right text-base"></i>
            </RouterLink>
          </div>
        </div>
      </section>
    </div>
  </div>
</template>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Noto+Serif+Thai:wght@500;700;900&family=IBM+Plex+Sans+Thai:wght@400;500;600;700&display=swap');

.font-display {
  font-family: 'Noto Serif Thai', serif;
}
.font-body {
  font-family: 'IBM Plex Sans Thai', sans-serif;
}

.scrollbar-none::-webkit-scrollbar {
  display: none;
}
.scrollbar-none {
  -ms-overflow-style: none;
  scrollbar-width: none;
}

/* Short laptop screens (e.g. 1280x720): compact the hero so it still fits one screen */
@media (min-width: 1024px) and (max-height: 800px) {
  .hero-inner {
    padding-top: 1.25rem;
    padding-bottom: 5.5rem;
  }
  .hero-title {
    font-size: 2.6rem;
    line-height: 1;
  }
  .hero-lead {
    display: none;
  }
  .hero-story-img {
    aspect-ratio: 21 / 9;
  }
}

.hero-rise {
  animation: hero-rise 0.8s cubic-bezier(0.22, 1, 0.36, 1) backwards;
}
@keyframes hero-rise {
  from {
    opacity: 0;
    transform: translateY(24px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@media (prefers-reduced-motion: reduce) {
  * {
    transition-duration: 0.01ms !important;
  }
  .hero-rise {
    animation: none;
  }
}
</style>
