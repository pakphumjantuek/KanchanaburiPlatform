<template>
  <div v-if="content" class="min-h-screen bg-[#F7F0E6] text-[#332820] pb-16">
    <!-- BREADCRUMB BAR -->
    <div class="bg-[#FFF9F2] border-b border-[#E8D9C9] py-3.5 shadow-2xs">
      <div
        class="mx-auto max-w-[1800px] px-4 sm:px-6 lg:px-10 text-sm text-[#4A3E35] flex items-center gap-2 font-semibold"
      >
        <RouterLink to="/" class="hover:text-[#D96C2C] transition">หน้าแรก</RouterLink>
        <i class="mdi mdi-chevron-right text-[#E8D9C9]"></i>
        <RouterLink to="/contents" class="hover:text-[#D96C2C] transition"
          >สำรวจคอนเทนต์</RouterLink
        >
        <i class="mdi mdi-chevron-right text-[#E8D9C9]"></i>
        <span class="text-[#D96C2C] font-bold">{{
          content.contentCategoryName || 'ท่องเที่ยวและวัฒนธรรม'
        }}</span>
        <i class="mdi mdi-chevron-right text-[#E8D9C9]"></i>
        <span class="text-[#332820] font-bold line-clamp-1">{{ content.title }}</span>
      </div>
    </div>

    <!-- MAIN 2-COLUMN LAYOUT WRAPPER (8 Cols Stream + 4 Cols Sticky Sidebar) -->
    <main class="mx-auto max-w-[1800px] px-4 sm:px-6 lg:px-10 py-8 space-y-10">
      <div class="grid grid-cols-1 items-start gap-8 lg:grid-cols-[minmax(0,1fr)_24rem] xl:grid-cols-[minmax(0,1fr)_28rem]">
        <!-- LEFT MAIN COLUMN -->
        <div class="min-w-0 space-y-8">
          <!-- 1. Video Player & Metadata Card -->
          <ContentDetailHero
            :content="content"
            :isFavorite="isFavorite"
            :changingFavorite="changingFavorite"
            @toggle-favorite="toggleFavorite"
            @share="shareContent"
          />

          <!-- 2. About this video -->
          <section
            v-if="content.summary"
            v-reveal
            class="rounded-3xl border border-[#E8D9C9] bg-white p-6 sm:p-8 shadow-sm space-y-4"
          >
            <div class="flex items-center gap-3 border-b border-[#E8D9C9] pb-4">
              <div class="h-10 w-10 rounded-xl bg-[#FFF1E8] text-[#D96C2C] flex items-center justify-center font-bold shadow-xs shrink-0">
                <i class="mdi mdi-text-box-outline text-xl"></i>
              </div>
              <div>
                <h2 class="text-xl sm:text-2xl font-black text-[#332820]">เกี่ยวกับเรื่องราวนี้</h2>
                <p class="text-xs sm:text-sm text-[#786B62] font-semibold">เรื่องราวและสาระสำคัญของสถานที่นี้</p>
              </div>
            </div>
            <p class="text-base sm:text-lg leading-relaxed text-[#332820] font-normal whitespace-pre-line">
              {{ content.summary }}
            </p>
          </section>

          <!-- 2.5 SPECIAL CATEGORY INFO CARD (ตามข้อกำหนด 8 หมวดหมู่) -->
          <section
            v-if="visibleSpecialFields.length"
            v-reveal
            class="rounded-3xl border border-[#E8D9C9] bg-white p-6 sm:p-8 shadow-sm space-y-5 relative overflow-hidden"
          >
            <div class="flex items-center gap-3 border-b border-[#E8D9C9] pb-4">
              <div
                class="h-10 w-10 rounded-xl bg-[#D96C2C] text-white flex items-center justify-center font-bold shadow-xs shrink-0"
              >
                <i class="mdi mdi-information-outline text-xl text-white"></i>
              </div>
              <div>
                <h3 class="text-xl sm:text-2xl font-black text-[#332820]">
                  {{ categoryRule.specialInfoTitle }}
                </h3>
                <p class="text-xs sm:text-sm text-[#786B62] font-semibold">
                  ข้อมูลไฮไลท์เฉพาะสำหรับหมวดหมู่ {{ content.contentCategoryName }}
                </p>
              </div>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-3.5 pt-1">
              <div
                v-for="field in visibleSpecialFields"
                :key="field.key"
                class="rounded-2xl border border-[#E8D9C9] bg-[#FFF9F2] p-4 space-y-1.5 shadow-2xs hover:border-[#D96C2C] transition"
              >
                <span
                  class="text-xs font-bold text-[#D96C2C] flex items-center gap-1.5 uppercase tracking-wide"
                >
                  <i class="mdi" :class="field.icon"></i>
                  {{ field.label }}
                </span>
                <p class="text-sm font-bold text-[#332820] line-clamp-2">
                  {{ field.value }}
                </p>
              </div>
            </div>
          </section>

          <!-- 3. Schedules & Events (ซ่อนเมื่อหมวดหมู่ระบุให้ซ่อน) -->
          <section
            v-if="shouldShowSchedule"
            v-reveal
            class="rounded-3xl bg-white border border-[#E8D9C9] p-6 sm:p-8 shadow-sm space-y-6"
          >
            <div class="flex flex-wrap items-center justify-between gap-3 border-b border-[#E8D9C9] pb-4">
              <div class="flex items-center gap-3">
                <div
                  class="h-10 w-10 rounded-xl bg-[#FFF1E8] text-[#D96C2C] flex items-center justify-center font-bold shadow-xs shrink-0"
                >
                  <i class="mdi mdi-calendar-clock-outline text-xl"></i>
                </div>
                <div>
                  <h2 class="text-xl sm:text-2xl font-black text-[#332820]">รอบการแสดง / กิจกรรม</h2>
                  <p class="text-xs sm:text-sm text-[#786B62] font-semibold">
                    รอบการแสดงสดและกิจกรรมวัฒนธรรมที่กำลังจะเกิดขึ้น
                  </p>
                </div>
              </div>
              <span
                class="text-xs font-bold text-[#D96C2C] bg-[#FFF9F2] border border-[#E8D9C9] px-4 py-1.5 rounded-full shadow-2xs"
              >
                มี {{ schedules.length }} รอบการแสดง
              </span>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div
                v-for="sch in schedules"
                :key="sch.scheduleId"
                class="rounded-2xl border border-[#E8D9C9] bg-[#FFF9F2] p-4.5 space-y-2 shadow-2xs hover:border-[#D96C2C] transition"
              >
                <span class="font-bold text-[#332820] text-base block">{{ sch.title }}</span>
                <p class="text-sm text-[#786B62] flex items-center gap-1.5 font-medium">
                  <i class="mdi mdi-clock-outline text-[#D96C2C]"></i>
                  <span>{{ formatDate(sch.startDateTime, { dateStyle: 'long' }) }}</span>
                </p>
                <p
                  v-if="sch.address"
                  class="text-sm text-[#786B62] flex items-center gap-1.5 font-normal"
                >
                  <i class="mdi mdi-map-marker-outline text-[#D96C2C]"></i>
                  <span>{{ sch.address }}</span>
                </p>
              </div>
            </div>
          </section>

          <!-- 4. Community Discussion & Reviews -->
          <ContentDetailReviews
            v-reveal
            v-model:comment="reviewForm.comment"
            :reviewData="reviewData"
            :myReview="myReview"
            :isLoggedIn="auth.isLoggedIn"
            :reviewSaving="reviewSaving"
            @save-review="saveReview"
            @remove-review="removeReview"
          />
        </div>

        <!-- RIGHT STICKY SIDEBAR (4 Cols: Location Map -> Shops/Products Widget) -->
        <div class="space-y-6 lg:sticky lg:top-28 lg:max-h-[calc(100vh-8rem)] lg:overflow-y-auto lg:pr-1">
          <ContentDetailSidebar
            :content="content"
            :shops="relatedShops"
            :products="products"
            @open-directions="openDirections"
          />
        </div>
      </div>

      <!-- BOTTOM SECTION: Recommended Related Video Cards -->
      <ContentDetailRelated v-reveal :items="relatedContents" />

      <!-- FOOTER SHARE / REPORT SECTION -->
      <section
        v-reveal
        class="flex flex-wrap items-center justify-between gap-4 rounded-3xl bg-white border border-[#E8D9C9] p-5 sm:p-6 shadow-sm"
      >
        <div class="flex flex-wrap items-center gap-3">
          <span class="text-sm font-black text-[#332820] flex items-center gap-2">
            <i class="mdi mdi-share-variant-outline text-[#D96C2C] text-lg"></i>
            แชร์คอนเทนต์นี้:
          </span>
          <div class="flex items-center gap-2">
            <button
              type="button"
              class="px-4 py-2 rounded-full bg-[#1877F2] text-white text-xs font-bold flex items-center gap-1.5 hover:opacity-90 transition cursor-pointer shadow-xs"
              @click="shareContent('fb')"
            >
              <i class="mdi mdi-facebook text-base"></i>
              <span>Facebook</span>
            </button>
            <button
              type="button"
              class="px-4 py-2 rounded-full bg-[#06C755] text-white text-xs font-bold flex items-center gap-1.5 hover:opacity-90 transition cursor-pointer shadow-xs"
              @click="shareContent('line')"
            >
              <i class="mdi mdi-forum-outline text-base"></i>
              <span>LINE</span>
            </button>
            <button
              type="button"
              class="px-4 py-2 rounded-full bg-[#FFF9F2] text-[#332820] border border-[#E8D9C9] text-xs font-bold flex items-center gap-1.5 hover:bg-[#F7F0E6] hover:border-[#D96C2C] transition cursor-pointer shadow-xs"
              @click="shareContent()"
            >
              <i class="mdi mdi-link-variant text-base text-[#D96C2C]"></i>
              <span>คัดลอกลิงก์</span>
            </button>
          </div>
        </div>

        <button
          type="button"
          class="px-4 py-2 rounded-full bg-[#FFF9F2] border border-[#E8D9C9] text-xs font-bold text-[#786B62] hover:text-rose-600 hover:border-rose-300 flex items-center gap-1.5 transition cursor-pointer shadow-xs"
          @click="openReport({ contentId: content.contentId, label: content.title })"
        >
          <i class="mdi mdi-flag-outline text-base"></i>
          <span>รายงานเนื้อหา</span>
        </button>
      </section>
    </main>

    <!-- REPORT MODAL -->
    <ContentReportModal
      v-model:reason="reportForm.reason"
      v-model:description="reportForm.description"
      :target="reportTarget"
      :reporting="reporting"
      @close="reportTarget = null"
      @submit="submitReport"
    />
  </div>

  <!-- SKELETON LOADING STATE -->
  <div v-else-if="loading" class="min-h-screen bg-[#F7F0E6] pb-16 animate-pulse">
    <div class="bg-[#FFF9F2] border-b border-[#E8D9C9] py-3 shadow-2xs">
      <div class="mx-auto max-w-[1800px] px-4 sm:px-6 lg:px-10">
        <div class="h-4 w-48 bg-[#E8D9C9]/50 rounded-lg"></div>
      </div>
    </div>

    <main class="mx-auto max-w-[1800px] px-4 sm:px-6 lg:px-10 py-8 space-y-10">
      <div class="grid grid-cols-1 items-start gap-8 lg:grid-cols-[minmax(0,1fr)_24rem] xl:grid-cols-[minmax(0,1fr)_28rem]">
        <div class="min-w-0 space-y-8">
          <div class="rounded-3xl border border-[#E8D9C9] bg-[#FFF9F2] p-6 space-y-4 shadow-xs">
            <div class="aspect-video w-full rounded-2xl bg-[#E8D9C9]/60"></div>
            <div class="h-7 w-3/4 rounded-xl bg-[#E8D9C9]/50"></div>
            <div class="h-4 w-1/3 rounded-lg bg-[#E8D9C9]/40"></div>
          </div>
          <div class="rounded-3xl border border-[#E8D9C9] bg-[#FFF9F2] p-6 space-y-3 shadow-xs">
            <div class="h-6 w-32 rounded-xl bg-[#E8D9C9]/50"></div>
            <div class="h-4 w-full rounded-lg bg-[#E8D9C9]/40"></div>
            <div class="h-4 w-2/3 rounded-lg bg-[#E8D9C9]/40"></div>
          </div>
        </div>
        <div class="space-y-6">
          <div
            class="rounded-3xl border border-[#E8D9C9] bg-[#FFF9F2] p-6 h-80 space-y-4 shadow-xs"
          >
            <div class="h-40 rounded-2xl bg-[#E8D9C9]/50"></div>
            <div class="h-5 w-2/3 rounded-xl bg-[#E8D9C9]/50"></div>
            <div class="h-4 w-1/2 rounded-lg bg-[#E8D9C9]/40"></div>
          </div>
        </div>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { formatDate } from '@/shared/utils/formatDate'
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  addContentFavorite,
  createReport,
  deleteContentReview,
  getContentSchedules,
  getContentFavoriteStatus,
  getContentReviews,
  getMyContentReview,
  getContentShopProducts,
  getMyContent,
  getPublicContent,
  getPublicContents,
  recordContentView,
  removeContentFavorite,
  saveContentReview,
  type ContentReview,
  type ContentReviews,
  type ContentSchedule,
  type ContentShopProduct,
  type PublicContent,
} from '@/features/contents/api'
import { getShop, getShops } from '@/features/shops/api/shopApi'
import type { Shop } from '@/features/shops/shared/types/shop'
import { useSwal } from '@/plugins/sweetalert'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useAuthStore } from '@/features/auth/stores/auth'

import { getCategoryRule } from '@/features/contents/constants/categoryRules'

import { vReveal } from '@/shared/directives/reveal'

import ContentDetailHero from '../components/ContentDetailHero.vue'
import ContentDetailReviews from '../components/ContentDetailReviews.vue'
import ContentDetailSidebar from '../components/ContentDetailSidebar.vue'
import ContentDetailRelated from '../components/ContentDetailRelated.vue'
import ContentReportModal from '../components/ContentReportModal.vue'

const route = useRoute()
const router = useRouter()
const swal = useSwal()
const auth = useAuthStore()

const content = ref<PublicContent | null>(null)
const schedules = ref<ContentSchedule[]>([])
const products = ref<ContentShopProduct[]>([])
const relatedShops = ref<Shop[]>([])
const relatedContents = ref<PublicContent[]>([])
const isFavorite = ref(false)
const changingFavorite = ref(false)

const categoryRule = computed(() => getCategoryRule(content.value?.contentCategoryName))

const shouldShowSchedule = computed(() => {
  if (categoryRule.value.hasSchedule === false) return false
  return schedules.value.length > 0
})

function getSpecialFieldValue(key: string): string | null {
  if (!content.value) return null
  const sch = schedules.value[0]
  const area = [content.value.subDistrictName, content.value.districtName].filter(Boolean).join(' ')
  const schedulePoint = sch?.title || (sch?.startDateTime ? formatDate(sch.startDateTime, { dateStyle: 'long' }) : null)

  switch (key) {
    case 'eventDate':
      return sch?.startDateTime ? formatDate(sch.startDateTime, { dateStyle: 'long' }) : null
    case 'eventTime':
    case 'showDateTime':
    case 'openHours':
    case 'openingHours':
    case 'openDays':
      return schedulePoint
    case 'location':
    case 'areaName':
      return sch?.address || area || null
    case 'locationMap':
      return content.value.latitude && content.value.longitude
        ? `GPS: ${content.value.latitude.toFixed(4)}, ${content.value.longitude.toFixed(4)}`
        : area || null
    case 'communityName':
    case 'informant':
    case 'instructor':
    case 'producer':
      return content.value.shopName || null
    case 'relatedShop':
      return content.value.shopName || relatedShops.value[0]?.shopName || null
    case 'relatedProducts':
      return products.value.length ? `${products.value.length} รายการสินค้าแนะนำ` : null
    default:
      // e.g. entranceFee: there is no data source for it, so show nothing rather than a guess
      return null
  }
}

// Only fields that actually have data are shown
const visibleSpecialFields = computed(() =>
  categoryRule.value.specialFields
    .map((field) => ({ ...field, value: getSpecialFieldValue(field.key) }))
    .filter((field) => field.value),
)

const reviewData = ref<ContentReviews>({ totalCount: 0, reviews: [] })
const myReview = ref<ContentReview | null>(null)
const reviewSaving = ref(false)
const reviewForm = reactive({ comment: '' })

const reportTarget = ref<{ contentId?: string; reviewId?: string; label: string } | null>(null)
const reporting = ref(false)
const reportForm = reactive({ reason: 'ข้อมูลไม่ถูกต้อง', description: '' })

const loading = ref(true)
const id = computed(() => (typeof route.params.id === 'string' ? route.params.id : ''))
const isOwnerPreview = computed(() => route.query.preview === 'mine')

async function load() {
  if (!id.value) return
  loading.value = true
  try {
    content.value = isOwnerPreview.value
      ? ((await getMyContent(id.value)) as PublicContent)
      : await getPublicContent(id.value)
    const [scheduleItems, productItems, reviews, shops, relatedData] = await Promise.all([
      getContentSchedules(id.value).catch(() => []),
      getContentShopProducts(id.value).catch(() => []),
      getContentReviews(id.value).catch(() => ({ totalCount: 0, reviews: [] })),
      content.value.shopId
        ? getShop(content.value.shopId)
            .then((shop) => [shop])
            .catch(() => [])
        : getShops({ districtId: content.value.districtId, pageSize: 3 }).catch(() => ({
            items: [] as Shop[],
          })),
      getPublicContents({ categoryId: content.value.contentCategoryId, pageSize: 5 }).catch(() => ({
        items: [],
      })),
    ])
    schedules.value = scheduleItems
    products.value = productItems
    reviewData.value = reviews
    relatedShops.value = 'items' in shops ? shops.items : (shops as Shop[])
    relatedContents.value = (relatedData.items || []).filter((item) => item.contentId !== id.value)

    if (!isOwnerPreview.value) {
      void recordContentView(id.value)
    }

    if (auth.isLoggedIn && !isOwnerPreview.value) {
      void getContentFavoriteStatus(id.value)
        .then((value) => {
          isFavorite.value = value
        })
        .catch(() => undefined)
      void getMyContentReview(id.value)
        .then((review) => {
          if (review) {
            myReview.value = review
            reviewForm.comment = review.comment
          }
        })
        .catch(() => undefined)
    }
  } catch (error) {
    await swal.error('ไม่พบคอนเทนต์', getApiErrorMessage(error, 'คอนเทนต์นี้อาจยังไม่ได้เผยแพร่'))
    await router.replace('/contents')
  } finally {
    loading.value = false
  }
}

async function toggleFavorite() {
  if (!auth.isLoggedIn) {
    await router.push('/login')
    return
  }
  changingFavorite.value = true
  try {
    if (isFavorite.value) await removeContentFavorite(id.value)
    else await addContentFavorite(id.value)
    isFavorite.value = !isFavorite.value
  } catch (error) {
    await swal.error('บันทึกรายการโปรดไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    changingFavorite.value = false
  }
}

async function saveReview() {
  if (!auth.isLoggedIn) {
    await router.push('/login')
    return
  }
  if (!reviewForm.comment.trim()) {
    await swal.warning('กรอกความคิดเห็น', 'กรุณาเขียนความคิดเห็นก่อนส่ง')
    return
  }
  reviewSaving.value = true
  try {
    myReview.value = await saveContentReview(id.value, { comment: reviewForm.comment })
    reviewData.value = await getContentReviews(id.value)
    await swal.success('บันทึกรีวิวแล้ว')
  } catch (error) {
    await swal.error('บันทึกรีวิวไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    reviewSaving.value = false
  }
}

async function removeReview() {
  if (!myReview.value) return
  const result = await swal.confirm('ลบรีวิวนี้หรือไม่?', 'การดำเนินการนี้ไม่สามารถย้อนกลับได้')
  if (!result.isConfirmed) return
  try {
    await deleteContentReview(myReview.value.reviewId)
    myReview.value = null
    reviewForm.comment = ''
    reviewData.value = await getContentReviews(id.value)
    await swal.success('ลบรีวิวแล้ว')
  } catch (error) {
    await swal.error('ลบรีวิวไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  }
}

async function shareContent(platform?: string) {
  const url = window.location.href
  if (platform === 'fb') {
    window.open(
      `https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(url)}`,
      '_blank',
      'noopener,noreferrer',
    )
  } else if (platform === 'line') {
    window.open(
      `https://social-plugins.line.me/lineit/share?url=${encodeURIComponent(url)}`,
      '_blank',
      'noopener,noreferrer',
    )
  } else {
    try {
      await navigator.clipboard.writeText(url)
      void swal.success('คัดลอกลิงก์แล้ว', 'นำลิงก์ไปแชร์ให้เพื่อน ๆ ได้เลย')
    } catch {
      await swal.error('คัดลอกลิงก์ไม่สำเร็จ', 'กรุณาคัดลอกจากแถบที่อยู่ของเบราว์เซอร์')
    }
  }
}

async function openReport(target: { contentId?: string; reviewId?: string; label: string }) {
  if (!auth.isLoggedIn) {
    await router.push('/login')
    return
  }
  reportTarget.value = target
  reportForm.reason = 'ข้อมูลไม่ถูกต้อง'
  reportForm.description = ''
}

async function submitReport() {
  if (!reportTarget.value || !reportForm.reason) return
  reporting.value = true
  try {
    await createReport({
      ...reportTarget.value,
      reason: reportForm.reason,
      description: reportForm.description || undefined,
    })
    reportTarget.value = null
    await swal.success('ส่งรายงานแล้ว', 'ทีมงานจะตรวจสอบรายการนี้ต่อไป')
  } catch (error) {
    await swal.error(
      'ส่งรายงานไม่สำเร็จ',
      getApiErrorMessage(error, 'คุณอาจเคยรายงานรายการนี้แล้ว'),
    )
  } finally {
    reporting.value = false
  }
}

function openDirections() {
  if (content.value?.latitude && content.value?.longitude) {
    window.open(
      `https://www.google.com/maps/dir/?api=1&destination=${content.value.latitude},${content.value.longitude}`,
      '_blank',
    )
  } else {
    const query = encodeURIComponent(
      `${content.value?.title || ''} ${content.value?.districtName || 'กาญจนบุรี'}`,
    )
    window.open(`https://www.google.com/maps/search/?api=1&query=${query}`, '_blank')
  }
}

watch(
  () => route.params.id,
  (newId, oldId) => {
    if (newId && newId !== oldId) {
      void load()
    }
  },
)

onMounted(load)
</script>
