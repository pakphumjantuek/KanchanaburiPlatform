<script setup lang="ts">
import { onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import http from '@/shared/api/http'

const route = useRoute()
const router = useRouter()

onMounted(async () => {
  // กลับมาจาก Stripe (วิธีชำระที่ต้อง redirect เช่น PromptPay/3DS): ซิงก์สถานะออเดอร์ก่อนไปหน้าโปรไฟล์
  const ids = String(route.query.orderIds ?? '').split(',').map((s) => s.trim()).filter(Boolean)
  if (route.query.redirect_status === 'succeeded' && ids.length) {
    try {
      if (ids.length === 1) await http.post(`/payments/orders/${ids[0]}/sync`)
      else await http.post('/payments/batch/sync', { orderIds: ids })
    } catch (error) {
      console.warn('Sync payment after redirect failed', error)
    }
  }
  router.replace({ path: '/profile', query: { tab: 'orders' } })
})
</script>

<template>
  <div class="min-h-screen bg-[#F7F0E6] flex items-center justify-center">
    <div class="flex items-center gap-2 text-[#D96C2C] font-black">
      <i class="mdi mdi-loading mdi-spin text-2xl"></i>
      <span>กำลังเปิดรายการคำสั่งซื้อ...</span>
    </div>
  </div>
</template>
