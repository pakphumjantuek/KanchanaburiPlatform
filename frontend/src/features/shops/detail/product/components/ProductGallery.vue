<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import type { Product } from '@/features/shops/api'
import { dealLabel } from '@/shared/utils/productDeal'

const props = defineProps<{
  product: Product
  galleryImages: string[]
}>()

const selectedImageIndex = defineModel<number>('selectedImageIndex', { default: 0 })

// a broken image URL should fall back to the placeholder instead of showing alt text
const mainFailed = ref(false)
watch([() => selectedImageIndex.value, () => props.galleryImages], () => (mainFailed.value = false))
const hasImages = computed(() => props.galleryImages.length > 0 && !mainFailed.value)
const currentImage = computed(
  () => props.galleryImages[selectedImageIndex.value] || props.galleryImages[0] || '',
)

function step(delta: number) {
  const n = props.galleryImages.length
  if (n < 2) return
  selectedImageIndex.value = (selectedImageIndex.value + delta + n) % n
}

// ---- lightbox (makes the "click to enlarge" hint real) ----
const lightboxOpen = ref(false)

function onKey(e: KeyboardEvent) {
  if (e.key === 'Escape') lightboxOpen.value = false
  else if (e.key === 'ArrowLeft') step(-1)
  else if (e.key === 'ArrowRight') step(1)
}

watch(lightboxOpen, (open) => {
  if (open) {
    window.addEventListener('keydown', onKey)
    document.body.style.overflow = 'hidden'
  } else {
    window.removeEventListener('keydown', onKey)
    document.body.style.overflow = ''
  }
})

onBeforeUnmount(() => {
  window.removeEventListener('keydown', onKey)
  document.body.style.overflow = ''
})
</script>

<template>
  <div class="space-y-4 lg:sticky lg:top-28">
    <!-- Main preview -->
    <div
      class="group relative aspect-4/3 overflow-hidden rounded-3xl border border-[#E8D9C9] bg-[#F7F0E6] shadow-xl shadow-[#D96C2C]/10"
    >
      <button
        v-if="hasImages"
        type="button"
        class="block h-full w-full cursor-zoom-in focus-visible:ring-4 focus-visible:ring-[#D96C2C]/40 focus-visible:outline-none focus-visible:ring-inset"
        :aria-label="`ขยายรูป ${product.productName}`"
        @click="lightboxOpen = true"
      >
        <img
          :src="currentImage"
          :alt="product.productName"
          class="h-full w-full object-cover transition-transform duration-700 ease-out group-hover:scale-105"
          :class="product.quantity <= 0 ? 'grayscale' : ''"
          @error="mainFailed = true"
        />
      </button>
      <div v-else class="flex h-full w-full items-center justify-center text-[#E8D9C9]">
        <i class="mdi mdi-image-outline text-7xl"></i>
      </div>

      <span
        v-if="product.activeDeal"
        class="absolute top-3 left-3 flex items-center gap-1 rounded-full bg-rose-600 px-3.5 py-1 text-xs font-black text-white shadow-lg"
      >
        <i class="mdi mdi-lightning-bolt-outline"></i>
        {{ dealLabel(product) }}
      </span>

      <template v-if="galleryImages.length > 1">
        <button
          type="button"
          class="absolute top-1/2 left-3 flex h-10 w-10 -translate-y-1/2 cursor-pointer items-center justify-center rounded-full bg-white/90 text-[#332820] shadow-lg transition hover:bg-white focus-visible:ring-4 focus-visible:ring-[#D96C2C]/40 focus-visible:outline-none"
          aria-label="รูปก่อนหน้า"
          @click="step(-1)"
        >
          <i class="mdi mdi-chevron-left text-2xl"></i>
        </button>
        <button
          type="button"
          class="absolute top-1/2 right-3 flex h-10 w-10 -translate-y-1/2 cursor-pointer items-center justify-center rounded-full bg-white/90 text-[#332820] shadow-lg transition hover:bg-white focus-visible:ring-4 focus-visible:ring-[#D96C2C]/40 focus-visible:outline-none"
          aria-label="รูปถัดไป"
          @click="step(1)"
        >
          <i class="mdi mdi-chevron-right text-2xl"></i>
        </button>
      </template>

      <div v-if="hasImages" class="pointer-events-none absolute right-3 bottom-3">
        <span
          class="flex items-center gap-1.5 rounded-full border border-white/20 bg-slate-950/75 px-3 py-1 text-[11px] font-bold text-white backdrop-blur-md"
        >
          <i class="mdi mdi-magnify-plus-outline text-sm text-[#F2A65A]"></i>
          คลิกเพื่อขยายรูป
        </span>
      </div>
    </div>

    <!-- Thumbnails -->
    <div v-if="galleryImages.length > 1" class="grid grid-cols-4 gap-3 sm:grid-cols-5">
      <button
        v-for="(img, idx) in galleryImages"
        :key="idx"
        type="button"
        class="relative aspect-square cursor-pointer overflow-hidden rounded-2xl border-2 transition duration-200 focus-visible:ring-4 focus-visible:ring-[#D96C2C]/30 focus-visible:outline-none"
        :class="
          selectedImageIndex === idx
            ? 'scale-95 border-[#D96C2C] shadow-md ring-4 ring-[#D96C2C]/20'
            : 'border-[#E8D9C9] opacity-80 hover:border-[#D96C2C] hover:opacity-100'
        "
        :aria-label="`ดูรูปที่ ${idx + 1}`"
        :aria-current="selectedImageIndex === idx"
        @click="selectedImageIndex = idx"
      >
        <img :src="img" :alt="`ภาพสินค้า ${idx + 1}`" class="h-full w-full object-cover" />
      </button>
    </div>

    <!-- Lightbox -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition-opacity duration-200"
        leave-active-class="transition-opacity duration-150"
        enter-from-class="opacity-0"
        leave-to-class="opacity-0"
      >
        <div
          v-if="lightboxOpen"
          class="fixed inset-0 z-[70] flex items-center justify-center bg-black/90 p-4"
          role="dialog"
          aria-modal="true"
          :aria-label="product.productName"
          @click.self="lightboxOpen = false"
        >
          <img
            :src="currentImage"
            :alt="product.productName"
            class="max-h-[88vh] max-w-full rounded-2xl object-contain shadow-2xl"
          />
          <button
            type="button"
            class="absolute top-4 right-4 flex h-11 w-11 cursor-pointer items-center justify-center rounded-full bg-white/15 text-white backdrop-blur-md transition hover:bg-white/30"
            aria-label="ปิด"
            @click="lightboxOpen = false"
          >
            <i class="mdi mdi-close text-2xl"></i>
          </button>
          <template v-if="galleryImages.length > 1">
            <button
              type="button"
              class="absolute left-3 flex h-12 w-12 cursor-pointer items-center justify-center rounded-full bg-white/15 text-white backdrop-blur-md transition hover:bg-white/30"
              aria-label="รูปก่อนหน้า"
              @click="step(-1)"
            >
              <i class="mdi mdi-chevron-left text-3xl"></i>
            </button>
            <button
              type="button"
              class="absolute right-3 flex h-12 w-12 cursor-pointer items-center justify-center rounded-full bg-white/15 text-white backdrop-blur-md transition hover:bg-white/30"
              aria-label="รูปถัดไป"
              @click="step(1)"
            >
              <i class="mdi mdi-chevron-right text-3xl"></i>
            </button>
            <span
              class="absolute bottom-4 rounded-full bg-black/60 px-3 py-1 text-xs font-bold text-white"
            >
              {{ selectedImageIndex + 1 }} / {{ galleryImages.length }}
            </span>
          </template>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>
