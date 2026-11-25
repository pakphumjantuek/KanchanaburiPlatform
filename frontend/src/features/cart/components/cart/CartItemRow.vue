<script setup lang="ts">
import type { CartItem } from '../../api/cartApi'

interface Props {
  item: CartItem
  updating: boolean
}

const props = defineProps<Props>()

const emit = defineEmits<{
  'update-quantity': [quantity: number]
  'remove': []
}>()

const apiOrigin = (import.meta.env.VITE_API_URL ?? 'https://localhost:7289/api').replace(
  /\/api$/,
  '',
)

function imageUrl(url?: string) {
  return url?.startsWith('/') ? `${apiOrigin}${url}` : url
}
</script>

<template>
  <article
    class="flex flex-col gap-3 rounded-2xl border border-[#E8D9C9] bg-white p-3.5 transition hover:border-[#D96C2C]/50 md:flex-row md:items-center md:gap-5"
  >
    <div class="flex min-w-0 flex-1 items-center gap-4">
      <div class="h-20 w-20 shrink-0 overflow-hidden rounded-xl border border-[#E8D9C9] bg-[#F7F0E6]">
        <img
          v-if="props.item.imageUrl"
          :src="imageUrl(props.item.imageUrl)"
          :alt="props.item.productName"
          class="h-full w-full object-cover"
        />
        <div v-else class="flex h-full w-full items-center justify-center text-[#C9B8A8]">
          <i class="mdi mdi-image-off-outline text-2xl"></i>
        </div>
      </div>

      <div class="min-w-0 flex-1 space-y-1">
        <h2 class="line-clamp-2 text-sm leading-snug font-black text-[#332820] sm:text-base">
          {{ props.item.productName }}
        </h2>
        <div v-if="props.item.dealType" class="flex flex-wrap items-center gap-2">
          <span class="inline-flex items-center gap-1 rounded-full bg-rose-600 px-2 py-0.5 text-[10px] font-black text-white">
            <i class="mdi mdi-lightning-bolt"></i>
            {{ props.item.dealType === 'FlashDeal' ? 'โปรโมชันพิเศษ' : 'โปรเด็ดชุมชน' }}
          </span>
          <span v-if="props.item.originalPrice" class="text-xs font-semibold text-[#A8988C] line-through">
            ฿{{ Number(props.item.originalPrice).toLocaleString('th-TH') }}
          </span>
        </div>
        <!-- deal / normal breakdown, compact -->
        <p v-if="props.item.dealQuantity > 0" class="text-[11px] font-semibold text-rose-700">
          โปร {{ props.item.dealQuantity }} ชิ้น × ฿{{ Number(props.item.dealUnitPrice).toLocaleString('th-TH') }}
        </p>
        <p v-if="props.item.normalQuantity > 0" class="text-[11px] font-semibold text-[#786B62]">
          ปกติ {{ props.item.normalQuantity }} ชิ้น × ฿{{ Number(props.item.normalUnitPrice).toLocaleString('th-TH') }}
        </p>
      </div>
    </div>

    <div class="flex items-center justify-between gap-4 md:justify-end md:gap-6">
      <div class="inline-flex items-center rounded-full border border-[#E8D9C9] bg-[#F7F0E6] p-1">
        <button
          type="button"
          class="flex h-8 w-8 cursor-pointer items-center justify-center rounded-full bg-white text-[#332820] shadow-sm transition hover:bg-[#FFF1E8] hover:text-[#D96C2C] disabled:cursor-not-allowed disabled:opacity-40"
          :disabled="props.updating || props.item.quantity <= 1"
          aria-label="ลดจำนวน"
          @click="emit('update-quantity', props.item.quantity - 1)"
        >
          <i class="mdi mdi-minus"></i>
        </button>
        <span class="min-w-[38px] text-center text-sm font-black text-[#332820]">
          <i v-if="props.updating" class="mdi mdi-loading mdi-spin text-sm text-[#D96C2C]"></i>
          <span v-else>{{ props.item.quantity }}</span>
        </span>
        <button
          type="button"
          class="flex h-8 w-8 cursor-pointer items-center justify-center rounded-full bg-white text-[#332820] shadow-sm transition hover:bg-[#FFF1E8] hover:text-[#D96C2C] disabled:cursor-not-allowed disabled:opacity-40"
          :disabled="props.updating"
          aria-label="เพิ่มจำนวน"
          @click="emit('update-quantity', props.item.quantity + 1)"
        >
          <i class="mdi mdi-plus"></i>
        </button>
      </div>

      <span class="w-24 text-right text-lg font-black text-[#D96C2C] sm:text-xl">
        ฿{{ Number(props.item.subtotal).toLocaleString('th-TH') }}
      </span>

      <button
        type="button"
        class="flex h-9 w-9 shrink-0 cursor-pointer items-center justify-center rounded-full text-[#786B62] transition hover:bg-rose-50 hover:text-rose-600 focus-visible:ring-4 focus-visible:ring-rose-200 focus-visible:outline-none active:scale-95"
        title="ลบออกจากตะกร้า"
        aria-label="ลบออกจากตะกร้า"
        @click="emit('remove')"
      >
        <i class="mdi mdi-trash-can-outline text-xl"></i>
      </button>
    </div>
  </article>
</template>
