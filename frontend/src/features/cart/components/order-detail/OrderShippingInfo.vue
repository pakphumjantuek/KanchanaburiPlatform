<script setup lang="ts">
export interface Shipment {
  shippingProvider?: string
  trackingNumber?: string
  shippingStatus: string
  shippedAt?: string
  deliveredAt?: string
}

interface Props {
  shippingMethod: string
  receiverName?: string
  receiverPhone?: string
  shippingAddress?: string
  shipment?: Shipment
}

defineProps<Props>()
</script>

<template>
  <div class="space-y-4">
    <!-- Delivery address -->
    <div class="rounded-3xl border border-[#E8D9C9] bg-white p-5 sm:p-6">
      <h2 class="mb-3 flex items-center gap-2 text-base font-black text-[#332820]">
        <i class="mdi mdi-map-marker-outline text-xl text-[#D96C2C]"></i>
        ที่อยู่จัดส่ง
      </h2>
      <div class="space-y-1.5 rounded-2xl bg-[#FFF9F2] p-4">
        <p class="text-sm font-black text-[#332820]">{{ receiverName || '-' }}</p>
        <p class="flex items-center gap-1.5 text-sm font-semibold text-[#786B62]">
          <i class="mdi mdi-phone-outline text-[#D96C2C]"></i>{{ receiverPhone || '-' }}
        </p>
        <p class="whitespace-pre-line text-sm leading-relaxed font-semibold text-[#786B62]">
          {{ shippingAddress || '-' }}
        </p>
      </div>
    </div>

    <!-- Shipment tracking (once the shop has shipped) -->
    <div v-if="shipment" class="rounded-3xl border border-sky-200 bg-sky-50/70 p-5 sm:p-6">
      <h2 class="mb-3 flex items-center gap-2 text-base font-black text-sky-900">
        <i class="mdi mdi-truck-check-outline text-xl text-sky-600"></i>
        ข้อมูลการจัดส่งพัสดุ
      </h2>
      <dl class="space-y-2 text-sm font-semibold">
        <div class="flex justify-between gap-3">
          <dt class="text-sky-800/80">ขนส่ง</dt>
          <dd class="font-black text-sky-950">{{ shipment.shippingProvider || 'รอร้านระบุ' }}</dd>
        </div>
        <div class="flex items-center justify-between gap-3">
          <dt class="text-sky-800/80">เลขพัสดุ</dt>
          <dd class="rounded-lg bg-white px-2.5 py-1 font-mono font-black text-sky-900">
            {{ shipment.trackingNumber || 'รอร้านแจ้งเลขพัสดุ' }}
          </dd>
        </div>
        <div class="flex justify-between gap-3">
          <dt class="text-sky-800/80">สถานะ</dt>
          <dd class="font-black" :class="shipment.shippingStatus === 'Delivered' ? 'text-emerald-700' : 'text-sky-900'">
            {{ shipment.shippingStatus === 'Delivered' ? 'ส่งถึงผู้รับแล้ว' : 'ระหว่างขนส่ง' }}
          </dd>
        </div>
      </dl>
    </div>
  </div>
</template>
