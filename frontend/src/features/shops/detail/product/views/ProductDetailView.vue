<script setup lang="ts">
import { formatDate } from '@/shared/utils/formatDate'
import { computed, ref, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  getProductCategories,
  getProductReviews,
  getPublicProduct,
  getPublicProducts,
  getShop,
  type Product,
  type ProductCategory,
  type ProductReviews,
  type Shop,
} from '@/features/shops/api'
import { push } from 'notivue'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useCartStore } from '@/stores/cart'
import { useAuthStore } from '@/features/auth/stores/auth'

// Sub-components
import ProductGallery from '../components/ProductGallery.vue'
import ProductInfoPanel from '../components/ProductInfoPanel.vue'
import ProductSellerCard from '../components/ProductSellerCard.vue'
import ProductMiniCard from '../components/ProductMiniCard.vue'
import { vReveal } from '@/shared/directives/reveal'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const cartStore = useCartStore()

const product = ref<Product | null>(null)
const shop = ref<Shop | null>(null)
const otherShopProducts = ref<Product[]>([])
const otherShopTotal = ref(0)
const reviewData = ref<ProductReviews>({ totalCount: 0, averageRating: 0, reviews: [] })

const selectedImageIndex = ref(0)
const quantity = ref(1)
const activeTab = ref<'details' | 'reviews'>('details')
const categories = ref<ProductCategory[]>([])

const loading = ref(true)
const adding = ref(false)
const buyingNow = ref(false)

const apiOrigin = (import.meta.env.VITE_API_URL ?? 'https://localhost:7289/api').replace(
  /\/api$/,
  '',
)

function imageUrl(url?: string) {
  if (!url) return undefined
  return url.startsWith('/') ? `${apiOrigin}${url}` : url
}

function formatPrice(value: number) {
  return new Intl.NumberFormat('th-TH', {
    style: 'currency',
    currency: 'THB',
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(value)
}

const galleryImages = computed(() => {
  const list: string[] = []
  if (product.value?.imageUrl) list.push(imageUrl(product.value.imageUrl)!)
  product.value?.detailImages?.forEach((img) => {
    if (img) list.push(imageUrl(img)!)
  })
  return list
})

function productCategoryName(productCategoryId: string) {
  return categories.value.find((c) => c.productCategoryId === productCategoryId)?.categoryName ?? ''
}

function scrollToTop() {
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

function goToReviews() {
  activeTab.value = 'reviews'
  document.getElementById('product-tabs')?.scrollIntoView({ behavior: 'smooth' })
}

const maxQuantity = computed(() => {
  if (!product.value) return 1
  const dealQuantity = product.value.activeDeal?.availableQuantity
  return dealQuantity ?? product.value.quantity
})

function openMapDirections() {
  if (shop.value?.latitude != null && shop.value.longitude != null) {
    window.open(
      `https://www.google.com/maps/dir/?api=1&destination=${shop.value.latitude},${shop.value.longitude}`,
      '_blank',
    )
  }
}

async function addToCart() {
  if (!product.value) return
  if (!auth.isLoggedIn) {
    await router.push('/login')
    return
  }
  adding.value = true
  try {
    await cartStore.add(product.value.productId, quantity.value)
    push.success({
      title: 'เพิ่มลงตะกร้าเรียบร้อย',
      message: `เพิ่ม ${product.value.productName} จำนวน ${quantity.value} ชิ้น เรียบร้อยแล้ว`,
    })
  } catch (error) {
    push.error({
      title: 'เพิ่มสินค้าไม่สำเร็จ',
      message: getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'),
    })
  } finally {
    adding.value = false
  }
}

async function buyNow() {
  if (!product.value) return
  if (!auth.isLoggedIn) {
    await router.push('/login')
    return
  }
  buyingNow.value = true
  try {
    await cartStore.add(product.value.productId, quantity.value)
    await router.push('/cart')
  } catch (error) {
    push.error({
      title: 'ไม่สามารถดำเนินการได้',
      message: getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'),
    })
  } finally {
    buyingNow.value = false
  }
}

function getRatingPercent(star: number) {
  if (!reviewData.value.totalCount || !reviewData.value.ratingCounts) return 0
  const count = reviewData.value.ratingCounts[star] || 0
  return Math.round((count / reviewData.value.totalCount) * 100)
}



// The same component instance is reused when the URL changes from one product to another
// (e.g. clicking a related product), so the data must be reloaded whenever the id changes.
let loadToken = 0

async function loadProduct(id: string, isFirstLoad = false) {
  const token = ++loadToken
  loading.value = true
  quantity.value = 1
  selectedImageIndex.value = 0
  activeTab.value = 'details'
  if (!isFirstLoad) window.scrollTo({ top: 0, behavior: 'smooth' })
  try {
    const currentProduct = await getPublicProduct(id)
    if (token !== loadToken) return
    const [currentShop, allProducts, productCategories, reviews] = await Promise.all([
      getShop(currentProduct.shopId),
      getPublicProducts(),
      getProductCategories().catch(() => [] as ProductCategory[]),
      getProductReviews(currentProduct.productId).catch(() => ({
        totalCount: 0,
        averageRating: 0,
        ratingCounts: { 5: 0, 4: 0, 3: 0, 2: 0, 1: 0 },
        reviews: [],
      })),
    ])
    if (token !== loadToken) return
    product.value = currentProduct
    shop.value = currentShop
    categories.value = productCategories
    reviewData.value = reviews
    const sameShop = allProducts.filter(
      (item) => item.productId !== currentProduct.productId && item.shopId === currentProduct.shopId,
    )
    otherShopTotal.value = sameShop.length
    otherShopProducts.value = sameShop.slice(0, 4)
  } catch (error) {
    push.error({
      title: 'ไม่พบสินค้า',
      message: getApiErrorMessage(error, 'สินค้านี้อาจถูกปิดการขายหรือไม่มีอยู่ในระบบ'),
    })
    await router.replace('/shops')
  } finally {
    if (token === loadToken) loading.value = false
  }
}

onMounted(() => loadProduct(String(route.params.id), true))

watch(
  () => route.params.id,
  (newId, oldId) => {
    // ignore changes caused by leaving this page for another route that also has an :id
    if (newId && newId !== oldId && route.path.startsWith('/products/')) {
      void loadProduct(String(newId))
    }
  },
)
</script>

<template>
  <div
    v-if="product && shop"
    class="min-h-screen bg-[#F7F0E6] pb-24 text-[#332820] lg:pb-16"
  >
    <!-- BREADCRUMB -->
    <div class="border-b border-[#E8D9C9] bg-[#FFF9F2] py-3.5 shadow-2xs">
      <div
        class="scrollbar-none mx-auto flex w-full max-w-[1800px] items-center gap-2 overflow-x-auto px-4 text-xs font-bold whitespace-nowrap text-[#786B62] sm:px-6 lg:px-10"
      >
        <RouterLink to="/" class="shrink-0 transition hover:text-[#D96C2C]">หน้าแรก</RouterLink>
        <i class="mdi mdi-chevron-right shrink-0 text-[#E8D9C9]"></i>
        <RouterLink to="/shops" class="shrink-0 transition hover:text-[#D96C2C]"
          >ร้านค้าทั้งหมด</RouterLink
        >
        <i class="mdi mdi-chevron-right shrink-0 text-[#E8D9C9]"></i>
        <RouterLink
          :to="`/shops/${shop.shopId}`"
          class="shrink-0 transition hover:text-[#D96C2C]"
        >
          {{ shop.shopName }}
        </RouterLink>
        <i class="mdi mdi-chevron-right shrink-0 text-[#E8D9C9]"></i>
        <span class="shrink-0 font-black text-[#D96C2C]">{{ product.productName }}</span>
      </div>
    </div>

    <!-- PRODUCT HERO: gallery + buy card -->
    <section
      class="relative border-b border-[#E8D9C9] bg-gradient-to-br from-white via-[#FFF9F2] to-[#F7F0E6] py-6 sm:py-8"
    >
      <div class="mx-auto w-full max-w-[1800px] px-4 sm:px-6 lg:px-10">
        <div class="grid grid-cols-1 gap-8 lg:grid-cols-12 lg:items-stretch lg:gap-12">
          <div class="lg:col-span-6">
            <ProductGallery
              v-model:selected-image-index="selectedImageIndex"
              :product="product"
              :gallery-images="galleryImages"
            />
          </div>

          <div class="lg:col-span-6">
            <ProductInfoPanel
              v-model:quantity="quantity"
              :product="product"
              :shop="shop"
              :adding="adding"
              :buying-now="buyingNow"
              :max-quantity="maxQuantity"
              :image-url="imageUrl"
              :format-price="formatPrice"
              :category-name="productCategoryName(product.productCategoryId)"
              :average-rating="reviewData.averageRating"
              :review-count="reviewData.totalCount"
              @add-to-cart="addToCart"
              @buy-now="buyNow"
              @go-reviews="goToReviews"
            />
          </div>
        </div>
      </div>
    </section>

    <!-- TABS -->
    <section
      id="product-tabs"
      class="sticky top-16 z-30 border-b border-[#E8D9C9] bg-white/95 shadow-sm shadow-[#332820]/5 backdrop-blur-md sm:top-20"
    >
      <div class="mx-auto w-full max-w-[1800px] px-4 sm:px-6 lg:px-10">
        <div class="scrollbar-none flex items-center gap-8 overflow-x-auto text-sm font-black">
          <button
            type="button"
            class="shrink-0 cursor-pointer border-b-4 py-4 transition focus-visible:outline-none"
            :class="
              activeTab === 'details'
                ? 'border-[#D96C2C] text-[#D96C2C]'
                : 'border-transparent text-[#786B62] hover:text-[#332820]'
            "
            @click="activeTab = 'details'"
          >
            รายละเอียดสินค้า
          </button>
          <button
            type="button"
            class="flex shrink-0 cursor-pointer items-center gap-1.5 border-b-4 py-4 transition focus-visible:outline-none"
            :class="
              activeTab === 'reviews'
                ? 'border-[#D96C2C] text-[#D96C2C]'
                : 'border-transparent text-[#786B62] hover:text-[#332820]'
            "
            @click="activeTab = 'reviews'"
          >
            <span>รีวิวสินค้า</span>
            <span
              v-if="reviewData.totalCount"
              class="rounded-full bg-[#FFF9F2] px-2 py-0.5 text-xs font-black text-[#D96C2C] ring-1 ring-[#E8D9C9]"
            >
              {{ reviewData.totalCount }}
            </span>
          </button>
        </div>
      </div>
    </section>

    <!-- BODY -->
    <main class="mx-auto w-full max-w-[1800px] px-4 pt-8 pb-12 sm:px-6 lg:px-10">
      <div class="grid grid-cols-1 items-start gap-8 lg:grid-cols-12">
        <div class="space-y-8 lg:col-span-8">
                    <!-- DETAILS -->
          <section
            v-if="activeTab === 'details'"
            v-reveal
            class="overflow-hidden rounded-3xl border border-[#E8D9C9] bg-white"
          >
            <!-- Heading -->
            <div class="relative border-b border-[#E8D9C9] bg-gradient-to-r from-[#FFF1E8] via-[#FFF9F2] to-white px-6 py-6 sm:px-8">
              <div class="absolute inset-y-0 left-0 w-1.5 bg-gradient-to-b from-[#D96C2C] to-[#F2A65A]"></div>
              <p class="text-xs font-black tracking-[0.18em] text-[#D96C2C]">รายละเอียด</p>
              <h2 class="mt-1 text-xl leading-[1.35] font-black tracking-tight text-[#332820] sm:text-2xl">
                เกี่ยวกับ {{ product.productName }}
              </h2>
            </div>

            <div class="space-y-8 px-6 py-7 sm:px-8 sm:py-8">
              <!-- Description -->
              <div v-if="product.description" class="max-w-3xl">
                <p class="text-base leading-[1.9] font-medium whitespace-pre-line text-[#4A3E35] sm:text-lg">
                  {{ product.description }}
                </p>
              </div>
              <div
                v-else
                class="flex flex-col items-center gap-2 rounded-2xl border-2 border-dashed border-[#E8D9C9] bg-[#FFF9F2] px-6 py-10 text-center"
              >
                <i class="mdi mdi-text-box-outline text-4xl text-[#C9B8A8]"></i>
                <p class="text-sm font-semibold text-[#786B62]">ร้านค้ายังไม่ได้ระบุรายละเอียดของสินค้านี้</p>
              </div>

              <!-- Quick facts -->
              <div>
                <h3 class="mb-3 text-sm font-black text-[#332820]">ข้อมูลสินค้า</h3>
                <dl class="grid grid-cols-1 gap-3 sm:grid-cols-2">
                  <div
                    v-if="productCategoryName(product.productCategoryId)"
                    class="flex items-center gap-3.5 rounded-2xl bg-[#FFF9F2] p-4"
                  >
                    <span class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-white text-xl text-[#D96C2C] shadow-sm">
                      <i class="mdi mdi-tag-outline"></i>
                    </span>
                    <div class="min-w-0">
                      <dt class="text-xs font-bold text-[#786B62]">หมวดหมู่สินค้า</dt>
                      <dd class="truncate text-sm font-black text-[#332820] sm:text-base">
                        {{ productCategoryName(product.productCategoryId) }}
                      </dd>
                    </div>
                  </div>

                  <div class="flex items-center gap-3.5 rounded-2xl bg-[#FFF9F2] p-4">
                    <span class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-white text-xl text-[#D96C2C] shadow-sm">
                      <i class="mdi mdi-storefront-outline"></i>
                    </span>
                    <div class="min-w-0">
                      <dt class="text-xs font-bold text-[#786B62]">ร้านผู้จำหน่าย</dt>
                      <dd class="truncate text-sm font-black text-[#332820] sm:text-base">{{ shop.shopName }}</dd>
                    </div>
                  </div>

                  <div v-if="shop.districtName" class="flex items-center gap-3.5 rounded-2xl bg-[#FFF9F2] p-4">
                    <span class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-white text-xl text-[#D96C2C] shadow-sm">
                      <i class="mdi mdi-map-marker-outline"></i>
                    </span>
                    <div class="min-w-0">
                      <dt class="text-xs font-bold text-[#786B62]">พื้นที่</dt>
                      <dd class="text-sm font-black text-[#332820] sm:text-base">
                        {{ shop.subDistrictName ? `ต.${shop.subDistrictName} ` : '' }}อ.{{ shop.districtName }}
                        จ.กาญจนบุรี
                      </dd>
                    </div>
                  </div>

                  <div class="flex items-center gap-3.5 rounded-2xl bg-[#FFF9F2] p-4">
                    <span class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-white text-xl text-[#D96C2C] shadow-sm">
                      <i class="mdi mdi-package-variant-closed"></i>
                    </span>
                    <div class="min-w-0">
                      <dt class="text-xs font-bold text-[#786B62]">สถานะสินค้า</dt>
                      <dd
                        class="text-sm font-black sm:text-base"
                        :class="maxQuantity > 0 ? 'text-emerald-700' : 'text-rose-600'"
                      >
                        {{ maxQuantity > 0 ? `พร้อมจำหน่าย (เหลือ ${product.quantity} ชิ้น)` : 'สินค้าหมด' }}
                      </dd>
                    </div>
                  </div>
                </dl>
              </div>

              <!-- Extra photos (the first one is already the main picture) -->
              <div v-if="galleryImages.length > 1">
                <h3 class="mb-3 text-sm font-black text-[#332820]">ภาพประกอบเพิ่มเติม</h3>
                <div class="grid grid-cols-2 gap-3 sm:grid-cols-3">
                  <button
                    v-for="(img, idx) in galleryImages.slice(1)"
                    :key="idx"
                    type="button"
                    class="group relative aspect-4/3 cursor-pointer overflow-hidden rounded-2xl bg-[#F7F0E6] focus-visible:ring-4 focus-visible:ring-[#D96C2C]/30 focus-visible:outline-none"
                    :aria-label="`ดูรูปที่ ${idx + 2}`"
                    @click="selectedImageIndex = idx + 1; scrollToTop()"
                  >
                    <img
                      :src="img"
                      :alt="`ภาพประกอบ ${idx + 2}`"
                      class="h-full w-full object-cover transition duration-500 group-hover:scale-105"
                    />
                  </button>
                </div>
              </div>
            </div>
          </section>
<!-- REVIEWS -->
          <section
            v-if="activeTab === 'reviews'"
            v-reveal
            class="space-y-6 rounded-3xl border border-[#E8D9C9] bg-white p-6 shadow-md shadow-[#D96C2C]/5 sm:p-8"
          >
            <div class="flex items-center gap-3.5 border-b border-[#E8D9C9] pb-5">
              <div
                class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-[#FFF1E8] text-[#D96C2C]"
              >
                <i class="mdi mdi-star-face text-2xl"></i>
              </div>
              <h2 class="text-xl font-black tracking-tight text-[#332820] sm:text-2xl">
                รีวิวจากผู้ซื้อ
              </h2>
            </div>

            <!-- Rating summary: only when there are reviews -->
            <div
              v-if="reviewData.totalCount"
              class="grid grid-cols-1 items-center gap-6 rounded-2xl border border-[#E8D9C9] bg-[#FFF9F2] p-6 sm:grid-cols-12"
            >
              <div
                class="flex flex-col items-center justify-center space-y-2 border-b border-[#E8D9C9] pb-4 text-center sm:col-span-5 sm:border-r sm:border-b-0 sm:pr-4 sm:pb-0"
              >
                <span class="text-4xl font-black text-[#D96C2C] sm:text-5xl">
                  {{ reviewData.averageRating.toFixed(1) }}
                </span>
                <div class="flex items-center gap-1">
                  <i
                    v-for="star in 5"
                    :key="star"
                    :class="[
                      'mdi text-lg sm:text-xl',
                      star <= Math.round(reviewData.averageRating)
                        ? 'mdi-star text-amber-400'
                        : 'mdi-star-outline text-[#E8D9C9]',
                    ]"
                  ></i>
                </div>
                <span class="text-xs font-bold text-[#786B62]">
                  จากทั้งหมด {{ reviewData.totalCount }} รีวิว
                </span>
              </div>

              <div class="space-y-2 text-xs font-bold text-[#332820] sm:col-span-7">
                <div v-for="star in [5, 4, 3, 2, 1]" :key="star" class="flex items-center gap-3">
                  <span class="flex w-12 shrink-0 items-center gap-1 font-black">
                    {{ star }} <i class="mdi mdi-star text-sm text-amber-400"></i>
                  </span>
                  <div class="h-2.5 flex-1 overflow-hidden rounded-full border border-[#E8D9C9] bg-white">
                    <div
                      class="h-full rounded-full bg-[#D96C2C] transition-all duration-500"
                      :style="{ width: `${getRatingPercent(star)}%` }"
                    ></div>
                  </div>
                  <span class="w-10 text-right text-xs text-[#786B62]">
                    {{ reviewData.ratingCounts?.[star] || 0 }}
                  </span>
                </div>
              </div>
            </div>

            <div v-if="reviewData.reviews.length" class="space-y-4">
              <div
                v-for="rev in reviewData.reviews"
                :key="rev.reviewId"
                class="space-y-3 rounded-2xl border border-[#E8D9C9] bg-white p-5 shadow-2xs"
              >
                <div class="flex items-center justify-between gap-3">
                  <div class="flex items-center gap-3">
                    <div
                      class="flex h-9 w-9 items-center justify-center rounded-full border border-[#D96C2C]/30 bg-[#D96C2C]/15 text-sm font-black text-[#D96C2C] uppercase"
                    >
                      {{ rev.userName ? rev.userName.charAt(0) : 'U' }}
                    </div>
                    <div>
                      <h4 class="text-sm font-black text-[#332820]">{{ rev.userName }}</h4>
                      <p class="text-[11px] font-semibold text-[#786B62]">
                        {{ formatDate(rev.createdAt) }}
                      </p>
                    </div>
                  </div>
                  <div class="flex items-center gap-0.5">
                    <i
                      v-for="star in 5"
                      :key="star"
                      :class="[
                        'mdi text-sm',
                        star <= rev.rating ? 'mdi-star text-amber-400' : 'mdi-star-outline text-[#E8D9C9]',
                      ]"
                    ></i>
                  </div>
                </div>

                <p class="pl-1 text-xs leading-relaxed font-medium text-[#332820]/80 sm:text-sm">
                  {{ rev.comment }}
                </p>

                <div
                  v-if="rev.reply"
                  class="mt-3 space-y-1 rounded-xl border border-[#E8D9C9] bg-[#FFF9F2] p-3.5 text-xs"
                >
                  <span class="flex items-center gap-1 font-black text-[#D96C2C]">
                    <i class="mdi mdi-reply"></i> ตอบกลับจากร้านค้า:
                  </span>
                  <p class="font-semibold text-[#332820]">{{ rev.reply }}</p>
                </div>
              </div>
            </div>

            <div
              v-else
              class="space-y-3 rounded-2xl border-2 border-dashed border-[#E8D9C9] bg-[#FFF9F2] p-8 text-center"
            >
              <i class="mdi mdi-star-face block text-5xl text-[#D96C2C]/50"></i>
              <h3 class="text-base font-black text-[#332820]">ยังไม่มีรีวิวสำหรับสินค้านี้</h3>
              <p class="mx-auto max-w-sm text-xs font-semibold text-[#786B62]">
                เมื่อมีผู้ซื้อสินค้านี้และเขียนรีวิว ความคิดเห็นจะแสดงที่นี่
              </p>
            </div>
          </section>

          <!-- OTHER PRODUCTS FROM THIS SHOP -->
          <section
            v-if="otherShopProducts.length"
            v-reveal
            class="space-y-6 rounded-3xl border border-[#E8D9C9] bg-white p-6 shadow-md shadow-[#D96C2C]/5 sm:p-8"
          >
            <div
              class="flex flex-wrap items-center justify-between gap-4 border-b border-[#E8D9C9] pb-5"
            >
              <div class="flex items-center gap-3.5">
                <div
                  class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-[#FFF1E8] text-[#D96C2C]"
                >
                  <i class="mdi mdi-storefront-outline text-2xl"></i>
                </div>
                <div class="min-w-0">
                  <h2 class="text-xl font-black tracking-tight text-[#332820] sm:text-2xl">
                    สินค้าอื่นจากร้านนี้
                  </h2>
                  <p class="truncate text-xs font-semibold text-[#786B62] sm:text-sm">
                    จาก {{ shop.shopName }} · อีก {{ otherShopTotal }} รายการ
                  </p>
                </div>
              </div>
              <RouterLink
                :to="`/shops/${shop.shopId}/products`"
                class="inline-flex items-center gap-1.5 rounded-full border border-[#E8D9C9] bg-[#FFF9F2] px-4 py-2 text-xs font-bold text-[#D96C2C] transition hover:border-[#D96C2C] hover:bg-[#D96C2C] hover:text-white focus-visible:ring-4 focus-visible:ring-[#D96C2C]/20 focus-visible:outline-none active:scale-95 sm:text-sm"
              >
                <span>ดูทั้งหมด ({{ otherShopTotal }})</span>
                <i class="mdi mdi-arrow-right text-sm"></i>
              </RouterLink>
            </div>

            <div class="grid grid-cols-2 gap-4 sm:gap-5 md:grid-cols-4">
              <ProductMiniCard
                v-for="(op, idx) in otherShopProducts"
                :key="op.productId"
                v-reveal
                :style="{ animationDelay: `${idx * 80}ms` }"
                :product="op"
                :image-url="imageUrl"
                :format-price="formatPrice"
                :category-name="productCategoryName(op.productCategoryId)"
              />
            </div>
          </section>
        </div>

        <!-- SELLER CARD -->
        <div class="lg:col-span-4">
          <ProductSellerCard
            :shop="shop"
            :image-url="imageUrl"
            @open-directions="openMapDirections"
          />
        </div>
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
