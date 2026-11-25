<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getProductReviews, type ProductReviews } from '@/features/shops/api/productApi'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { formatDate } from '@/shared/utils/formatDate'

const props = defineProps<{ productId: string; productName: string }>()
const emit = defineEmits<{ close: [] }>()
const dialog = ref<HTMLDialogElement | null>(null)
const loading = ref(true)
const error = ref('')
const data = ref<ProductReviews | null>(null)

async function load() {
  loading.value = true
  error.value = ''
  try {
    data.value = await getProductReviews(props.productId)
  } catch (cause) {
    error.value = getApiErrorMessage(cause, 'กรุณาลองใหม่อีกครั้ง')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  dialog.value?.showModal()
  void load()
})
</script>

<template>
  <Teleport to="body">
    <dialog
      ref="dialog"
      aria-labelledby="product-reviews-title"
      class="m-auto max-h-[85dvh] w-[calc(100%_-_2rem)] max-w-2xl overflow-y-auto rounded-3xl border border-slate-200 bg-white p-0 text-slate-800 shadow-2xl backdrop:bg-slate-950/60"
      @close="emit('close')"
      @click="
        (event) => {
          if (event.target === dialog) dialog?.close()
        }
      "
    >
      <div class="p-6">
        <header class="flex items-start justify-between gap-4 border-b border-slate-100 pb-4">
          <div class="min-w-0">
            <div class="flex items-center gap-2">
              <span class="inline-flex h-8 w-8 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600">
                <i class="mdi mdi-star-box text-xl" aria-hidden="true"></i>
              </span>
              <h2 id="product-reviews-title" class="text-lg font-bold text-slate-900">รีวิวสินค้า</h2>
            </div>
            <p class="mt-1.5 break-words text-sm font-medium text-slate-500">{{ productName }}</p>
          </div>
          <button
            type="button"
            autofocus
            aria-label="ปิดหน้าต่างรีวิว"
            class="rounded-xl p-2 text-slate-400 hover:bg-slate-100 hover:text-slate-700 transition-colors focus:outline-none"
            @click="dialog?.close()"
          >
            <i class="mdi mdi-close text-xl" aria-hidden="true"></i>
          </button>
        </header>

        <div v-if="loading" role="status" class="py-16 text-center text-slate-500">
          <div class="inline-block h-8 w-8 animate-spin rounded-full border-2 border-emerald-500 border-t-transparent mb-3"></div>
          <p class="text-sm font-medium">กำลังโหลดข้อมูลรีวิว...</p>
        </div>
        <div v-else-if="error" role="alert" class="py-12 text-center">
          <div class="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-2xl bg-rose-50 text-rose-500">
            <i class="mdi mdi-alert-circle text-2xl"></i>
          </div>
          <p class="font-bold text-slate-900">โหลดรีวิวไม่สำเร็จ</p>
          <p class="mt-1 text-sm text-slate-500">{{ error }}</p>
          <button
            type="button"
            class="mt-4 rounded-xl bg-emerald-600 px-5 py-2 font-semibold text-white hover:bg-emerald-700 shadow-sm transition-colors focus:outline-none"
            @click="load"
          >
            ลองใหม่อีกครั้ง
          </button>
        </div>
        <template v-else-if="data">
          <!-- Summary rating bar -->
          <div class="my-5 rounded-2xl border border-slate-100 bg-gradient-to-r from-emerald-50/50 via-teal-50/30 to-amber-50/40 p-5 flex flex-col sm:flex-row items-center justify-between gap-4">
            <div class="flex items-center gap-4">
              <div class="flex h-14 w-14 items-center justify-center rounded-2xl bg-white shadow-sm border border-emerald-100">
                <span class="text-2xl font-black text-emerald-700">{{ data.averageRating.toFixed(1) }}</span>
              </div>
              <div>
                <div class="flex items-center gap-1 text-amber-400">
                  <i
                    v-for="star in 5"
                    :key="star"
                    :class="star <= Math.round(data.averageRating) ? 'mdi mdi-star' : 'mdi mdi-star-outline'"
                    class="text-lg"
                    aria-hidden="true"
                  ></i>
                </div>
                <p class="text-xs text-slate-500 mt-0.5">คะแนนความพึงพอใจเฉลี่ย</p>
              </div>
            </div>
            <div class="text-right sm:border-l sm:border-slate-200 sm:pl-6">
              <span class="text-2xl font-bold text-slate-800">{{ data.totalCount }}</span>
              <span class="text-xs text-slate-500 ml-1">รีวิวทั้งหมด</span>
            </div>
          </div>

          <!-- Review list -->
          <div v-if="data.reviews.length" class="space-y-3.5 max-h-[50vh] overflow-y-auto pr-1">
            <article
              v-for="review in data.reviews"
              :key="review.reviewId"
              class="rounded-2xl border border-slate-200/80 bg-white p-4 shadow-sm hover:border-slate-300 transition-colors"
            >
              <div class="flex flex-wrap items-center justify-between gap-2">
                <div class="flex items-center gap-2.5">
                  <div class="flex h-8 w-8 items-center justify-center rounded-full bg-slate-100 text-xs font-bold text-slate-600">
                    {{ review.userName.charAt(0).toUpperCase() }}
                  </div>
                  <h3 class="font-bold text-slate-900 text-sm">{{ review.userName }}</h3>
                </div>
                <time class="text-xs text-slate-400" :datetime="review.createdAt">{{
                  formatDate(review.createdAt)
                }}</time>
              </div>

              <div class="mt-2.5 flex items-center gap-0.5 text-amber-400" :aria-label="`${review.rating} จาก 5 ดาว`">
                <i
                  v-for="star in 5"
                  :key="star"
                  :class="star <= review.rating ? 'mdi mdi-star' : 'mdi mdi-star-outline'"
                  class="text-base"
                  aria-hidden="true"
                ></i>
                <span class="ml-1.5 text-xs font-semibold text-slate-600">{{ review.rating }}/5</span>
              </div>

              <p class="mt-2 text-sm text-slate-700 whitespace-pre-wrap leading-relaxed">{{ review.comment }}</p>

              <!-- Merchant Reply -->
              <div v-if="review.reply" class="mt-3.5 rounded-xl border border-emerald-100 bg-emerald-50/50 p-3.5 text-sm">
                <div class="flex items-center gap-1.5 font-bold text-emerald-800 text-xs uppercase tracking-wide">
                  <i class="mdi mdi-reply text-sm"></i>
                  คำตอบจากร้านค้า
                </div>
                <p class="mt-1 whitespace-pre-wrap break-words text-slate-700 text-xs leading-relaxed">{{ review.reply }}</p>
              </div>
            </article>
          </div>
          <div v-else class="py-12 text-center text-slate-400">
            <i class="mdi mdi-comment-outline text-4xl block mb-2 opacity-50"></i>
            <p class="text-sm">ยังไม่มีรีวิวสำหรับสินค้านี้</p>
          </div>
        </template>
      </div>
    </dialog>
  </Teleport>
</template>

<style scoped>
button:focus,
button:focus-visible {
  outline: none !important;
  box-shadow: none !important;
}
</style>
