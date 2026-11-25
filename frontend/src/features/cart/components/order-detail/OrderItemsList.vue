<script setup lang="ts">
import { ref } from 'vue'
import WriteReviewModal from './WriteReviewModal.vue'

export interface OrderItem {
  orderItemId: string
  productId?: string
  productName: string
  quantity: number
  unitPrice: number
  totalPrice: number
}

interface Props {
  items: OrderItem[]
  totalItems: number
  orderId: string
  orderStatus?: string
  reviewedProductIds?: string[]
}

const props = withDefaults(defineProps<Props>(), {
  orderStatus: '',
  reviewedProductIds: () => [],
})

const emit = defineEmits<{
  (e: 'review-added'): void
}>()

const activeReviewItem = ref<OrderItem | null>(null)
const isModalOpen = ref(false)

function openReviewModal(item: OrderItem) {
  activeReviewItem.value = item
  isModalOpen.value = true
}

function handleReviewSuccess() {
  emit('review-added')
}
</script>

<template>
  <div class="rounded-3xl border border-[#E8D9C9] bg-white p-5 sm:p-7">
    <h2 class="mb-4 flex items-center gap-2 text-base font-black text-[#332820]">
      <i class="mdi mdi-package-variant-closed text-xl text-[#D96C2C]"></i>
      รายการสินค้า
      <span class="rounded-full bg-[#FFF1E8] px-2.5 py-0.5 text-xs font-black text-[#BF5720]">{{ totalItems }} ชิ้น</span>
    </h2>

    <div class="divide-y divide-[#E8D9C9]/70">
      <div
        v-for="item in items"
        :key="item.orderItemId"
        class="flex flex-col justify-between gap-3 py-4 first:pt-0 last:pb-0 sm:flex-row sm:items-center"
      >
        <div class="flex min-w-0 items-center gap-4">
          <span class="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl bg-[#FFF1E8] text-2xl text-[#D96C2C]">
            <i class="mdi mdi-shopping-outline"></i>
          </span>
          <div class="min-w-0 space-y-1">
            <h3 class="line-clamp-2 text-sm font-black text-[#332820] sm:text-base">{{ item.productName }}</h3>
            <p class="text-xs font-semibold text-[#786B62] sm:text-sm">
              ฿{{ Number(item.unitPrice).toLocaleString('th-TH') }} ×
              <span class="rounded-md bg-[#F7F0E6] px-1.5 py-0.5 font-black text-[#332820]">{{ item.quantity }}</span> ชิ้น
            </p>
          </div>
        </div>

        <div class="flex items-center justify-between gap-4 sm:justify-end">
          <template v-if="orderStatus === 'Completed'">
            <span
              v-if="item.productId && reviewedProductIds.includes(item.productId)"
              class="inline-flex items-center gap-1.5 rounded-full border border-emerald-200 bg-emerald-50 px-3 py-1.5 text-xs font-black text-emerald-800"
            >
              <i class="mdi mdi-star text-amber-500"></i>
              <span>รีวิวแล้ว</span>
            </span>

            <button
              v-else-if="item.productId"
              type="button"
              class="inline-flex cursor-pointer items-center gap-1.5 rounded-full bg-[#FFF1E8] px-4 py-2 text-xs font-black text-[#BF5720] transition hover:bg-[#D96C2C] hover:text-white active:scale-95"
              @click="openReviewModal(item)"
            >
              <i class="mdi mdi-star-outline"></i>
              <span>เขียนรีวิว</span>
            </button>
          </template>

          <div class="shrink-0 text-right text-base font-black text-[#332820] sm:text-lg">
            ฿{{ Number(item.totalPrice).toLocaleString('th-TH') }}
          </div>
        </div>
      </div>
    </div>

    <!-- Review Modal -->
    <WriteReviewModal
      v-if="activeReviewItem?.productId"
      :is-open="isModalOpen"
      :product-id="activeReviewItem.productId"
      :product-name="activeReviewItem.productName"
      :order-id="orderId"
      @close="isModalOpen = false"
      @success="handleReviewSuccess"
    />
  </div>
</template>
