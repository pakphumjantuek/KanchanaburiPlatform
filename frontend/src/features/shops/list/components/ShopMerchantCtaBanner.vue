<script setup lang="ts">
import { useAuthStore } from '@/features/auth/stores/auth'

const auth = useAuthStore()
</script>

<template>
  <section class="rounded-3xl bg-gradient-to-br from-[#2C221E] via-[#3A2A20] to-[#1C1613] text-white p-6 sm:p-10 border-2 border-[#E8D9C9] shadow-lg overflow-hidden relative">
    <!-- Background Glow Rings -->
    <div class="absolute -right-16 -bottom-16 w-80 h-80 rounded-full bg-[#D96C2C]/20 filter blur-3xl pointer-events-none"></div>
    <div class="absolute top-0 left-1/3 w-64 h-64 rounded-full bg-[#BF5720]/15 filter blur-2xl pointer-events-none"></div>

    <div class="relative z-10 flex flex-col lg:flex-row items-center justify-between gap-8">
      <div class="space-y-3.5 text-center lg:text-left max-w-2xl">
        <span class="inline-flex items-center gap-1.5 px-3.5 py-1 rounded-full text-xs font-bold bg-[#D96C2C] text-white shadow-xs">
          <i class="mdi mdi-store-plus text-xs"></i>
          สำหรับผู้ประกอบการ & ร้านค้าชุมชน
        </span>

        <h2 class="text-2xl sm:text-3xl font-black text-white tracking-tight leading-tight">
          เป็นเจ้าของธุรกิจ? เปิดร้านค้ากับเราวันนี้
        </h2>

        <p class="text-xs sm:text-sm text-[#F7F0E6]/90 leading-relaxed font-medium">
          เข้าถึงกลุ่มนักท่องเที่ยวและลูกค้าคุณภาพในจังหวัดกาญจนบุรี รับเครื่องมือจัดการร้านค้าครบวงจร เชื่อมโยงสินค้าสู่โลกออนไลน์
        </p>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-2.5 pt-1 text-xs font-bold text-[#F7F0E6]">
          <div class="flex items-center justify-center lg:justify-start gap-2">
            <i class="mdi mdi-check-circle text-[#D96C2C] text-base"></i>
            <span>สมัครเปิดร้านค้าฟรี ไม่มีค่าธรรมเนียมแอบแฝง</span>
          </div>
          <div class="flex items-center justify-center lg:justify-start gap-2">
            <i class="mdi mdi-check-circle text-[#D96C2C] text-base"></i>
            <span>อนุมัติเปิดร้านและพร้อมจำหน่ายรวดเร็ว</span>
          </div>
          <div class="flex items-center justify-center lg:justify-start gap-2">
            <i class="mdi mdi-check-circle text-[#D96C2C] text-base"></i>
            <span>ระบบจัดการคำสั่งซื้อและสต็อกสินค้าแม่นยำ</span>
          </div>
          <div class="flex items-center justify-center lg:justify-start gap-2">
            <i class="mdi mdi-check-circle text-[#D96C2C] text-base"></i>
            <span>โปรโมตร้านค้าผ่านคอนเทนต์ท่องเที่ยว</span>
          </div>
        </div>
      </div>

      <div class="flex flex-col sm:flex-row items-center gap-3.5 shrink-0 w-full sm:w-auto">
        <RouterLink
          :to="auth.isLoggedIn ? '/register-shop' : '/login'"
          class="w-full sm:w-auto text-center px-6 py-3 rounded-full bg-[#D96C2C] hover:bg-[#BF5720] text-white font-black text-xs sm:text-sm shadow-md transition active:scale-95 border border-[#D96C2C] cursor-pointer"
        >
          สมัครเปิดร้านค้าชุมชน (ฟรี)
        </RouterLink>

        <RouterLink
          to="/about"
          class="w-full sm:w-auto text-center px-6 py-3 rounded-full bg-white/10 hover:bg-white/20 text-white font-bold text-xs backdrop-blur-md border border-white/20 transition cursor-pointer"
        >
          ดูรายละเอียดเพิ่มเติม
        </RouterLink>
      </div>
    </div>
  </section>
</template>
