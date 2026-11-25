<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import type { Product, Shop } from '@/features/shops/api'
import { useDealCountdown } from '@/shared/composables/useDealCountdown'
import { dealLabel, dealPrice } from '@/shared/utils/productDeal'

const props = defineProps<{
  product: Product
  shop: Shop
  adding?: boolean
  buyingNow?: boolean
  maxQuantity: number
  imageUrl: (url?: string) => string | undefined
  formatPrice: (val: number) => string
  /** the product's own category (not the shop's) */
  categoryName?: string
  averageRating?: number
  reviewCount?: number
}>()

const quantity = defineModel<number>('quantity', { default: 1 })
const { isFinished: isDealFinished, remaining: dealRemaining } = useDealCountdown(
  () => props.product.activeDeal?.endsAt,
)

const emit = defineEmits<{
  (e: 'add-to-cart'): void
  (e: 'buy-now'): void
  (e: 'go-reviews'): void
}>()

const soldOut = computed(() => props.maxQuantity <= 0)

// How much the customer saves when a deal is active
const savedAmount = computed(() => Math.max(0, props.product.price - dealPrice(props.product)))
const savedPercent = computed(() =>
  props.product.price > 0 ? Math.round((savedAmount.value / props.product.price) * 100) : 0,
)

function increaseQty() {
  if (quantity.value < props.maxQuantity) quantity.value++
}
function decreaseQty() {
  if (quantity.value > 1) quantity.value--
}
function validateQty() {
  if (isNaN(quantity.value) || quantity.value < 1) quantity.value = 1
  else if (quantity.value > props.maxQuantity) quantity.value = Math.max(1, props.maxQuantity)
}

// ---- mobile sticky buy bar: shown while the main buttons are out of view ----
const ctaRef = ref<HTMLElement | null>(null)
const ctaVisible = ref(true)
const footerVisible = ref(false)
const showStickyBar = computed(() => !ctaVisible.value && !footerVisible.value)

let observer: IntersectionObserver | null = null
onMounted(() => {
  if (typeof IntersectionObserver === 'undefined') return
  observer = new IntersectionObserver((entries) => {
    for (const entry of entries) {
      if (entry.target === ctaRef.value) ctaVisible.value = entry.isIntersecting
      else footerVisible.value = entry.isIntersecting
    }
  })
  if (ctaRef.value) observer.observe(ctaRef.value)
  const footer = document.querySelector('footer')
  if (footer) observer.observe(footer)
})
onBeforeUnmount(() => observer?.disconnect())
</script>

<template>
  <div class="space-y-5">
    <!-- Chips -->
    <div class="flex flex-wrap items-center gap-2">
      <span
        v-if="product.activeDeal"
        class="inline-flex items-center gap-1 rounded-full bg-rose-600 px-3 py-1 text-xs font-black text-white shadow-md sm:text-sm"
      >
        <i class="mdi mdi-lightning-bolt-outline"></i>{{ dealLabel(product) }}
      </span>
      <span
        v-if="categoryName"
        class="inline-flex items-center gap-1 rounded-full border border-[#E8D9C9] bg-white px-3.5 py-1 text-xs font-bold text-[#D96C2C] shadow-2xs sm:text-sm"
      >
        <i class="mdi mdi-tag-outline"></i>
        {{ categoryName }}
      </span>
    </div>

    <h1 class="text-3xl leading-tight font-black tracking-tight text-[#332820] sm:text-4xl">
      {{ product.productName }}
    </h1>

    <!-- Seller + rating -->
    <div class="flex flex-wrap items-center gap-3">
      <RouterLink
        :to="`/shops/${shop.shopId}`"
        class="group inline-flex items-center gap-2.5 rounded-full border border-[#E8D9C9] bg-white py-1.5 pr-4 pl-1.5 shadow-2xs transition hover:border-[#D96C2C] focus-visible:ring-4 focus-visible:ring-[#D96C2C]/20 focus-visible:outline-none"
      >
        <img
          v-if="imageUrl(shop.coverImageUrl)"
          :src="imageUrl(shop.coverImageUrl)"
          :alt="shop.shopName"
          class="h-8 w-8 rounded-full border border-white object-cover"
        />
        <span
          v-else
          class="flex h-8 w-8 items-center justify-center rounded-full bg-[#FFF9F2] text-base text-[#D96C2C]"
        >
          <i class="mdi mdi-storefront-outline"></i>
        </span>
        <span class="text-sm font-bold text-[#332820] transition group-hover:text-[#D96C2C]">{{
          shop.shopName
        }}</span>
        <i class="mdi mdi-chevron-right text-sm text-[#D96C2C]"></i>
      </RouterLink>

      <button
        v-if="reviewCount"
        type="button"
        class="inline-flex cursor-pointer items-center gap-1.5 rounded-full border border-[#E8D9C9] bg-white px-3 py-1.5 text-sm font-bold text-[#332820] shadow-2xs transition hover:border-[#D96C2C]"
        @click="emit('go-reviews')"
      >
        <i class="mdi mdi-star text-base text-amber-500"></i>
        <span>{{ (averageRating ?? 0).toFixed(1) }}</span>
        <span class="font-semibold text-[#786B62]">({{ reviewCount }} รีวิว)</span>
      </button>
      <span v-else class="text-sm font-semibold text-[#786B62]">ยังไม่มีรีวิว</span>
    </div>

    <!-- Deal countdown -->
    <div
      v-if="product.activeDeal"
      class="flex flex-wrap items-center gap-3 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm font-bold text-rose-800"
    >
      <span v-if="!isDealFinished"
        ><i class="mdi mdi-clock-outline mr-1"></i>เหลือเวลา {{ dealRemaining }}</span
      >
      <span v-else><i class="mdi mdi-clock-remove-outline mr-1"></i>ดีลหมดเวลาแล้ว</span>
      <span v-if="!isDealFinished" class="rounded-full bg-white px-3 py-1"
        >เหลือ {{ product.activeDeal.availableQuantity }} สิทธิ์</span
      >
    </div>

    <!-- Buy card: price, stock, quantity, actions -->
    <div
      class="space-y-5 rounded-3xl border border-[#E8D9C9] bg-white p-5 shadow-lg shadow-[#D96C2C]/5 sm:p-6"
    >
      <div class="flex flex-wrap items-end justify-between gap-3">
        <div>
          <span class="mb-0.5 block text-xs font-bold text-[#786B62]">{{
            product.activeDeal ? 'ราคาดีล' : 'ราคา'
          }}</span>
          <div class="flex flex-wrap items-baseline gap-x-3">
            <span class="text-4xl font-black tracking-tight text-[#D96C2C] sm:text-5xl">{{
              formatPrice(dealPrice(product))
            }}</span>
            <span v-if="product.activeDeal" class="text-lg font-semibold text-[#786B62] line-through">{{
              formatPrice(product.price)
            }}</span>
            <span
              v-if="product.activeDeal && savedPercent > 0"
              class="rounded-full bg-rose-600 px-2.5 py-0.5 text-sm font-black text-white"
            >
              -{{ savedPercent }}%
            </span>
          </div>
          <p v-if="product.activeDeal && savedAmount > 0" class="mt-1 text-sm font-bold text-rose-700">
            ประหยัด {{ formatPrice(savedAmount) }}
          </p>
        </div>

        <span
          v-if="soldOut"
          class="inline-flex items-center gap-1.5 rounded-full border border-rose-200 bg-rose-50 px-3.5 py-1.5 text-xs font-bold text-rose-700 sm:text-sm"
        >
          <i class="mdi mdi-close-circle-outline"></i>สินค้าหมด
        </span>
        <span
          v-else-if="maxQuantity <= 5"
          class="inline-flex items-center gap-1.5 rounded-full border border-[#D96C2C]/30 bg-[#FFF9F2] px-3.5 py-1.5 text-xs font-bold text-[#BF5720] sm:text-sm"
        >
          <i class="mdi mdi-alert-circle-outline"></i>เหลือเพียง {{ maxQuantity }} ชิ้น
        </span>
        <span
          v-else
          class="inline-flex items-center gap-1.5 rounded-full border border-[#E8D9C9] bg-[#FFF9F2] px-3.5 py-1.5 text-xs font-bold text-[#332820] sm:text-sm"
        >
          <span class="h-2.5 w-2.5 rounded-full bg-emerald-600"></span>
          มีสินค้า {{ product.quantity }} ชิ้น
        </span>
      </div>

      <!-- Quantity -->
      <div class="flex flex-wrap items-center gap-x-4 gap-y-2 border-t border-[#E8D9C9] pt-5">
        <span class="shrink-0 text-sm font-bold text-[#332820] sm:text-base">จำนวน</span>
        <div
          class="flex items-center rounded-full border border-[#E8D9C9] bg-[#FFF9F2] p-1 shadow-2xs"
        >
          <button
            type="button"
            class="flex h-11 w-11 cursor-pointer items-center justify-center rounded-full bg-white text-[#332820] shadow-2xs transition hover:bg-[#E8D9C9]/50 focus-visible:ring-4 focus-visible:ring-[#D96C2C]/20 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-40"
            :disabled="quantity <= 1 || soldOut"
            aria-label="ลดจำนวน"
            @click="decreaseQty"
          >
            <i class="mdi mdi-minus text-lg"></i>
          </button>
          <input
            v-model.number="quantity"
            type="number"
            min="1"
            :max="Math.max(1, maxQuantity)"
            :disabled="soldOut"
            aria-label="จำนวนสินค้า"
            class="w-14 bg-transparent text-center text-lg font-black text-[#332820] outline-none disabled:opacity-50"
            @blur="validateQty"
          />
          <button
            type="button"
            class="flex h-11 w-11 cursor-pointer items-center justify-center rounded-full bg-white text-[#332820] shadow-2xs transition hover:bg-[#E8D9C9]/50 focus-visible:ring-4 focus-visible:ring-[#D96C2C]/20 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-40"
            :disabled="quantity >= maxQuantity || soldOut"
            aria-label="เพิ่มจำนวน"
            @click="increaseQty"
          >
            <i class="mdi mdi-plus text-lg"></i>
          </button>
        </div>
        <p class="basis-full text-xs font-medium text-[#786B62] sm:text-sm">
          <template v-if="product.activeDeal">
            มีสินค้า {{ product.quantity }} ชิ้น · ใช้สิทธิ์ดีลได้
            {{ product.activeDeal.availableQuantity }} ชิ้น
          </template>
          <template v-else>ซื้อได้สูงสุด {{ product.quantity }} ชิ้นต่อครั้ง</template>
        </p>
      </div>

      <!-- Actions -->
      <div ref="ctaRef" class="grid grid-cols-2 gap-3">
        <button
          type="button"
          class="flex cursor-pointer items-center justify-center gap-2 rounded-full border-2 border-[#D96C2C] bg-white px-3 py-3.5 text-sm font-black whitespace-nowrap text-[#D96C2C] transition hover:bg-[#FFF9F2] focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25 focus-visible:outline-none active:scale-95 disabled:cursor-not-allowed disabled:opacity-50 sm:text-base"
          :disabled="adding || soldOut"
          @click="emit('add-to-cart')"
        >
          <i class="mdi text-xl text-[#D96C2C]" :class="adding ? 'mdi-loading mdi-spin' : 'mdi-cart-plus'"></i>
          <span>เพิ่มลงตะกร้า</span>
        </button>

        <button
          type="button"
          class="flex cursor-pointer items-center justify-center gap-2 rounded-full bg-[#D96C2C] px-3 py-3.5 text-sm font-black whitespace-nowrap text-white shadow-lg shadow-[#D96C2C]/30 transition hover:bg-[#BF5720] focus-visible:ring-4 focus-visible:ring-[#D96C2C]/30 focus-visible:outline-none active:scale-95 disabled:cursor-not-allowed disabled:opacity-50 sm:text-base"
          :disabled="buyingNow || soldOut"
          @click="emit('buy-now')"
        >
          <i class="mdi text-xl text-white" :class="buyingNow ? 'mdi-loading mdi-spin' : 'mdi-lightning-bolt'"></i>
          <span class="text-white">{{ soldOut ? 'สินค้าหมด' : 'สั่งซื้อทันที' }}</span>
        </button>
      </div>
    </div>

    <!-- Why buy here (facts only) -->
    <ul class="grid gap-2 text-sm font-semibold text-[#4A3E35] sm:grid-cols-3">
      <li class="flex items-center gap-2 rounded-2xl bg-white px-3.5 py-3 ring-1 ring-[#E8D9C9]">
        <i class="mdi mdi-storefront-outline text-xl text-[#D96C2C]"></i>
        <span>สินค้าจากร้านชุมชนกาญจนบุรี</span>
      </li>
      <li class="flex items-center gap-2 rounded-2xl bg-white px-3.5 py-3 ring-1 ring-[#E8D9C9]">
        <i class="mdi mdi-credit-card-outline text-xl text-[#D96C2C]"></i>
        <span>จ่ายด้วยบัตรหรือโอนธนาคาร</span>
      </li>
      <li class="flex items-center gap-2 rounded-2xl bg-white px-3.5 py-3 ring-1 ring-[#E8D9C9]">
        <i class="mdi mdi-star-outline text-xl text-[#D96C2C]"></i>
        <span>อ่านรีวิวจากผู้ซื้อจริง</span>
      </li>
    </ul>
    <!-- Mobile sticky buy bar -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-200 ease-out"
        leave-active-class="transition duration-150 ease-in"
        enter-from-class="translate-y-full opacity-0"
        leave-to-class="translate-y-full opacity-0"
      >
        <div
          v-if="showStickyBar"
          class="fixed inset-x-0 bottom-0 z-40 flex items-center gap-3 border-t border-[#E8D9C9] bg-[#FFF9F2]/95 px-4 py-3 shadow-2xl backdrop-blur-md lg:hidden"
        >
          <div class="min-w-0 shrink-0">
            <span class="block text-[11px] font-bold text-[#786B62]">{{
              product.activeDeal ? 'ราคาดีล' : 'ราคา'
            }}</span>
            <span class="block text-xl leading-tight font-black text-[#D96C2C]">{{
              formatPrice(dealPrice(product))
            }}</span>
          </div>
          <button
            type="button"
            class="flex h-11 w-11 shrink-0 cursor-pointer items-center justify-center rounded-full border-2 border-[#D96C2C] bg-white text-[#D96C2C] transition active:scale-95 disabled:cursor-not-allowed disabled:opacity-50"
            :disabled="adding || soldOut"
            aria-label="เพิ่มลงตะกร้า"
            @click="emit('add-to-cart')"
          >
            <i class="mdi text-xl" :class="adding ? 'mdi-loading mdi-spin' : 'mdi-cart-plus'"></i>
          </button>
          <button
            type="button"
            class="flex h-11 flex-1 cursor-pointer items-center justify-center gap-1.5 rounded-full bg-[#D96C2C] text-sm font-black whitespace-nowrap text-white shadow-md transition active:scale-95 disabled:cursor-not-allowed disabled:opacity-50"
            :disabled="buyingNow || soldOut"
            @click="emit('buy-now')"
          >
            <i class="mdi mdi-lightning-bolt text-lg text-white"></i>
            <span class="text-white">{{ soldOut ? 'สินค้าหมด' : 'สั่งซื้อทันที' }}</span>
          </button>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<style scoped>
input[type='number']::-webkit-inner-spin-button,
input[type='number']::-webkit-outer-spin-button {
  -webkit-appearance: none;
  margin: 0;
}
input[type='number'] {
  -moz-appearance: textfield;
}
</style>
