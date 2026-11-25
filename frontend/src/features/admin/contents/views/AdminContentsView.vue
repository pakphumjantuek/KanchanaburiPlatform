<template>
  <main class="mx-auto w-full max-w-[1920px] space-y-6 px-4 py-6 sm:px-6 lg:px-10 lg:py-8">
    <!-- HEADER BANNER -->
    <section
      class="relative overflow-hidden rounded-3xl bg-gradient-to-br from-slate-900 via-slate-800 to-teal-900 p-6 text-white shadow-lg sm:p-8"
    >
      <div class="pointer-events-none absolute -right-16 -top-16 h-64 w-64 rounded-full bg-teal-400/20 blur-3xl" />
      <div class="pointer-events-none absolute -bottom-24 left-1/3 h-64 w-64 rounded-full bg-cyan-400/10 blur-3xl" />

      <div class="relative z-10 flex flex-col gap-6 md:flex-row md:items-center md:justify-between">
        <div class="flex items-center gap-4">
          <div
            class="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl border border-white/20 bg-white/10 text-3xl text-teal-300 backdrop-blur-md"
          >
            <i class="mdi mdi-shield-search" />
          </div>
          <div>
            <h1 class="text-2xl font-black tracking-tight sm:text-3xl">ตรวจสอบคอนเทนต์</h1>
            <p class="mt-1 text-sm font-bold text-slate-300">
              ดูเนื้อหาของผู้ใช้และร้านค้า อนุมัติ หรือระงับรายการที่ฝ่าฝืนกฎของระบบ
            </p>
          </div>
        </div>

        <button
          type="button"
          class="inline-flex cursor-pointer items-center gap-2 self-start rounded-2xl border border-white/25 bg-white/10 px-4 py-2.5 text-sm font-bold text-white backdrop-blur-md transition hover:bg-white/20 active:scale-95 md:self-auto"
          :disabled="loading"
          @click="refreshData"
        >
          <i class="mdi mdi-refresh text-lg" :class="{ 'animate-spin': loading }" />
          <span>รีเฟรช</span>
        </button>
      </div>
    </section>

    <!-- ALERT: มีรายการรอตรวจสอบ -->
    <div
      v-if="stats.pending > 0 && activeTab !== 'Pending'"
      class="flex flex-col gap-3 rounded-2xl border border-amber-300 bg-amber-50 p-4 sm:flex-row sm:items-center sm:justify-between"
    >
      <div class="flex items-center gap-3">
        <div class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-amber-200 text-2xl text-amber-800">
          <i class="mdi mdi-bell-ring-outline" />
        </div>
        <div>
          <p class="text-sm font-black text-amber-900">มี {{ stats.pending }} คอนเทนต์รอการตรวจสอบ</p>
          <p class="text-xs font-bold text-amber-800">ตรวจดูและอนุมัติเพื่อเผยแพร่บนหน้าเว็บไซต์</p>
        </div>
      </div>
      <button
        type="button"
        class="inline-flex cursor-pointer items-center justify-center gap-2 rounded-xl bg-amber-600 px-5 py-2.5 text-sm font-black text-white shadow-xs transition hover:bg-amber-700 active:scale-95"
        @click="selectTab('Pending')"
      >
        <span>ไปตรวจสอบเลย</span>
        <i class="mdi mdi-arrow-right text-lg" />
      </button>
    </div>

    <!-- KPI STAT CARDS (คลิกเพื่อกรอง) -->
    <section class="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
      <button
        v-for="card in statCards"
        :key="card.value"
        type="button"
        class="group relative cursor-pointer overflow-hidden rounded-2xl border bg-white p-4 text-left shadow-xs transition hover:-translate-y-0.5 hover:shadow-md"
        :class="activeTab === card.value ? [card.activeBorder, 'ring-4', card.ring] : 'border-slate-300'"
        @click="selectTab(card.value)"
      >
        <span class="absolute inset-y-0 left-0 w-1.5" :class="card.bar" />
        <div class="flex items-start justify-between gap-2 pl-1.5">
          <div class="min-w-0">
            <p class="truncate text-xs font-black tracking-wide text-slate-600">{{ card.label }}</p>
            <p class="mt-1.5 text-3xl font-black leading-none" :class="card.text">
              {{ getTabCount(card.value) }}
            </p>
          </div>
          <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl text-xl" :class="card.iconBox">
            <i :class="['mdi', card.icon]" />
          </div>
        </div>
        <p class="mt-3 truncate pl-1.5 text-xs font-bold text-slate-500">{{ card.hint }}</p>
      </button>
    </section>

    <!-- SEARCH & FILTER -->
    <section class="rounded-2xl border border-slate-300 bg-white p-3 shadow-xs sm:p-4">
      <div class="flex flex-col gap-3 xl:flex-row xl:items-center xl:justify-between">
        <div class="flex gap-1.5 overflow-x-auto rounded-2xl bg-slate-100 p-1.5">
          <button
            v-for="tab in statusTabs"
            :key="tab.value"
            type="button"
            class="inline-flex shrink-0 cursor-pointer items-center gap-2 rounded-xl px-3.5 py-2 text-xs font-bold transition sm:text-sm"
            :class="
              activeTab === tab.value
                ? 'bg-white text-teal-800 shadow-sm ring-1 ring-slate-300'
                : 'text-slate-600 hover:bg-white/60 hover:text-slate-900'
            "
            @click="selectTab(tab.value)"
          >
            <i :class="['mdi text-base', tab.icon]" />
            <span>{{ tab.label }}</span>
            <span
              class="rounded-full px-2 py-0.5 text-xs font-black"
              :class="activeTab === tab.value ? 'bg-teal-600 text-white' : 'bg-slate-200 text-slate-700'"
            >
              {{ getTabCount(tab.value) }}
            </span>
          </button>
        </div>

        <div class="flex flex-col gap-3 sm:flex-row sm:items-center">
          <select
            v-model="selectedCategory"
            class="w-full cursor-pointer rounded-2xl border border-slate-300 bg-slate-50 px-4 py-2.5 text-sm font-bold text-slate-800 focus:border-teal-600 focus:bg-white focus:outline-none focus:ring-4 focus:ring-teal-600/15 sm:w-56"
            @change="onFilterChange"
          >
            <option value="">ทุกหมวดหมู่คอนเทนต์</option>
            <option v-for="cat in categories" :key="cat.contentCategoryId" :value="cat.contentCategoryId">
              {{ cat.categoryName }}
            </option>
          </select>

          <div class="relative w-full sm:w-72">
            <i class="mdi mdi-magnify absolute left-4 top-1/2 -translate-y-1/2 text-xl text-slate-500" />
            <input
              v-model="searchQuery"
              type="text"
              placeholder="ค้นหาชื่อคอนเทนต์..."
              class="w-full rounded-2xl border border-slate-300 bg-slate-50 py-2.5 pl-11 pr-10 text-sm font-bold text-slate-900 placeholder:font-bold placeholder:text-slate-500 focus:border-teal-600 focus:bg-white focus:outline-none focus:ring-4 focus:ring-teal-600/15"
              @keydown.enter="onFilterChange"
            />
            <button
              v-if="searchQuery"
              type="button"
              class="absolute right-3 top-1/2 -translate-y-1/2 cursor-pointer text-slate-500 hover:text-slate-800"
              title="ล้างคำค้นหา"
              @click="clearSearch"
            >
              <i class="mdi mdi-close-circle text-lg" />
            </button>
          </div>

          <button
            type="button"
            class="inline-flex cursor-pointer items-center justify-center gap-1.5 rounded-2xl bg-teal-700 px-5 py-2.5 text-sm font-bold text-white shadow-xs transition hover:bg-teal-800 active:scale-95"
            @click="onFilterChange"
          >
            <i class="mdi mdi-magnify text-lg" />
            <span>ค้นหา</span>
          </button>
        </div>
      </div>
    </section>

    <!-- MAIN DATA TABLE -->
    <AppDataTable
      :columns="columns"
      :items="contents"
      row-key="contentId"
      :loading="loading"
      :pagination="pagination"
      :variant="'admin'"
      empty-message="ไม่พบข้อมูลคอนเทนต์ตามเงื่อนไขที่เลือก"
      @page-change="changePage"
    >
      <!-- TITLE & SUMMARY COLUMN -->
      <template #cell-title="{ item }">
        <div class="flex items-center gap-3.5 py-1.5">
          <div
            class="flex h-14 w-20 shrink-0 items-center justify-center overflow-hidden rounded-xl border border-slate-300 bg-gradient-to-br from-teal-50 to-slate-100 text-teal-700 shadow-xs"
          >
            <img
              v-if="thumbOf(asContent(item))"
              :src="thumbOf(asContent(item))"
              alt=""
              class="h-full w-full object-cover"
            />
            <i v-else class="mdi mdi-text-box-outline text-2xl" />
          </div>

          <div class="min-w-0">
            <p
              class="cursor-pointer truncate text-sm font-black text-slate-900 transition hover:text-teal-700"
              @click="openPreview(asContent(item))"
            >
              {{ asContent(item).title }}
            </p>
            <p v-if="asContent(item).summary" class="mt-0.5 line-clamp-1 text-xs font-bold text-slate-600">
              {{ asContent(item).summary }}
            </p>
            <div class="mt-1 flex flex-wrap items-center gap-2">
              <span
                v-if="asContent(item).contentCategoryName"
                class="rounded-lg border border-teal-200 bg-teal-50 px-2 py-0.5 text-xs font-bold text-teal-800"
              >
                {{ asContent(item).contentCategoryName }}
              </span>
              <span
                v-if="asContent(item).youtubeUrl"
                class="inline-flex items-center gap-1 rounded-lg border border-rose-200 bg-rose-50 px-2 py-0.5 text-xs font-bold text-rose-700"
              >
                <i class="mdi mdi-youtube text-sm" /> วิดีโอ
              </span>
              <span class="font-mono text-xs font-bold text-slate-500">
                #{{ asContent(item).contentId.slice(0, 8) }}
              </span>
            </div>
          </div>
        </div>
      </template>

      <!-- CREATOR / SHOP -->
      <template #cell-shop="{ item }">
        <span
          v-if="asContent(item).shopName"
          class="inline-flex items-center gap-1.5 rounded-xl border border-teal-200 bg-teal-50 px-3 py-1.5 text-xs font-black text-teal-800"
        >
          <i class="mdi mdi-storefront-outline text-sm" />
          {{ asContent(item).shopName }}
        </span>
        <span
          v-else
          class="inline-flex items-center gap-1.5 rounded-xl border border-slate-300 bg-slate-100 px-3 py-1.5 text-xs font-black text-slate-700"
        >
          <i class="mdi mdi-account-outline text-sm" />
          ผู้ใช้ทั่วไป
        </span>
      </template>

      <!-- STATUS BADGE -->
      <template #cell-status="{ item }">
        <span
          class="inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-black"
          :class="statusBadgeClass(asContent(item).status)"
        >
          <i :class="['mdi text-sm', statusIcon(asContent(item).status)]" />
          {{ statusLabel(asContent(item).status) }}
        </span>
      </template>

      <!-- ACTIONS COLUMN -->
      <template #cell-actions="{ item }">
        <div class="flex flex-wrap items-center gap-2">
          <button
            type="button"
            class="inline-flex cursor-pointer items-center gap-1.5 rounded-xl border border-slate-300 bg-white px-3 py-2 text-xs font-bold text-slate-700 shadow-xs transition hover:border-teal-600 hover:bg-teal-50 hover:text-teal-800"
            title="ดูรายละเอียดคอนเทนต์"
            @click="openPreview(asContent(item))"
          >
            <i class="mdi mdi-eye-outline text-base" />
            <span>ดูข้อมูล</span>
          </button>

          <button
            v-if="asContent(item).status === 'Pending'"
            type="button"
            class="inline-flex cursor-pointer items-center gap-1.5 rounded-xl bg-emerald-700 px-3 py-2 text-xs font-bold text-white shadow-xs transition hover:bg-emerald-800 active:scale-95"
            title="อนุมัติและเผยแพร่คอนเทนต์"
            @click="publish(asContent(item))"
          >
            <i class="mdi mdi-check-circle-outline text-base" />
            <span>อนุมัติ</span>
          </button>

          <button
            v-if="asContent(item).status === 'Archived'"
            type="button"
            class="inline-flex cursor-pointer items-center gap-1.5 rounded-xl bg-emerald-700 px-3 py-2 text-xs font-bold text-white shadow-xs transition hover:bg-emerald-800 active:scale-95"
            title="ปลดการระงับและนำกลับมาเผยแพร่"
            @click="restore(asContent(item))"
          >
            <i class="mdi mdi-shield-check-outline text-base" />
            <span>ปลดระงับ</span>
          </button>

          <button
            v-else
            type="button"
            class="inline-flex cursor-pointer items-center gap-1.5 rounded-xl border border-rose-300 bg-rose-50 px-3 py-2 text-xs font-bold text-rose-800 transition hover:bg-rose-100"
            title="ระงับคอนเทนต์เมื่อทำผิดระเบียบ"
            @click="archive(asContent(item))"
          >
            <i class="mdi mdi-shield-alert-outline text-base" />
            <span>ระงับ</span>
          </button>
        </div>
      </template>
    </AppDataTable>

    <!-- PREVIEW MODAL -->
    <Teleport to="body">
      <div
        v-if="previewItem"
        class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/70 p-4 backdrop-blur-sm"
        @click.self="previewItem = null"
      >
        <div
          class="relative max-h-[92vh] w-full max-w-2xl overflow-y-auto rounded-3xl border border-slate-300 bg-white shadow-2xl"
        >
          <!-- MODAL HEADER -->
          <div class="relative bg-gradient-to-br from-slate-900 via-slate-800 to-teal-900 p-6 text-white">
            <button
              type="button"
              class="absolute right-4 top-4 flex h-9 w-9 cursor-pointer items-center justify-center rounded-xl bg-white/10 text-white transition hover:bg-white/25"
              title="ปิด"
              @click="previewItem = null"
            >
              <i class="mdi mdi-close text-xl" />
            </button>

            <span
              class="mb-2 inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-black"
              :class="statusBadgeClass(previewItem.status)"
            >
              <i :class="['mdi text-sm', statusIcon(previewItem.status)]" />
              {{ statusLabel(previewItem.status) }}
            </span>
            <h2 class="pr-10 text-xl font-black sm:text-2xl">{{ previewItem.title }}</h2>
            <p class="mt-1 font-mono text-xs font-bold text-slate-300">#{{ previewItem.contentId.slice(0, 8) }}</p>
          </div>

          <!-- MODAL CONTENT -->
          <div class="space-y-4 p-6 text-sm text-slate-700">
            <div
              v-if="thumbOf(previewItem)"
              class="overflow-hidden rounded-2xl border border-slate-300 bg-slate-100"
            >
              <img :src="thumbOf(previewItem)" alt="" class="aspect-video w-full object-cover" />
            </div>

            <div v-if="previewItem.summary" class="rounded-2xl border border-slate-300 bg-slate-50 p-4">
              <p class="mb-1 flex items-center gap-1.5 text-xs font-black text-slate-600">
                <i class="mdi mdi-text-box-outline text-base text-teal-700" /> เนื้อหาย่อ / คำอธิบาย
              </p>
              <p class="whitespace-pre-line font-bold leading-relaxed text-slate-800">{{ previewItem.summary }}</p>
            </div>

            <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <div class="rounded-2xl border border-slate-300 bg-white p-4">
                <p class="flex items-center gap-1.5 text-xs font-black text-slate-600">
                  <i class="mdi mdi-shape-outline text-base text-teal-700" /> หมวดหมู่
                </p>
                <p class="mt-1.5 font-black text-slate-900">{{ previewItem.contentCategoryName || '-' }}</p>
              </div>
              <div class="rounded-2xl border border-slate-300 bg-white p-4">
                <p class="flex items-center gap-1.5 text-xs font-black text-slate-600">
                  <i class="mdi mdi-storefront-outline text-base text-teal-700" /> ร้านค้า / ผู้สร้าง
                </p>
                <p class="mt-1.5 font-black text-slate-900">{{ previewItem.shopName || 'ผู้ใช้ทั่วไป' }}</p>
              </div>
              <div class="rounded-2xl border border-slate-300 bg-white p-4">
                <p class="flex items-center gap-1.5 text-xs font-black text-slate-600">
                  <i class="mdi mdi-map-marker-outline text-base text-teal-700" /> สถานที่ (อำเภอ / ตำบล)
                </p>
                <p class="mt-1.5 font-black text-slate-900">
                  {{ previewItem.districtName || '-' }} / {{ previewItem.subDistrictName || '-' }}
                </p>
              </div>
              <div class="rounded-2xl border border-slate-300 bg-white p-4">
                <p class="flex items-center gap-1.5 text-xs font-black text-slate-600">
                  <i class="mdi mdi-crosshairs-gps text-base text-teal-700" /> พิกัดแผนที่
                </p>
                <p class="mt-1.5 font-black text-slate-900">
                  <span v-if="previewItem.latitude && previewItem.longitude">
                    {{ previewItem.latitude }}, {{ previewItem.longitude }}
                  </span>
                  <span v-else class="font-bold text-slate-500">ไม่ได้ระบุพิกัด</span>
                </p>
              </div>
            </div>

            <div v-if="previewItem.youtubeUrl" class="rounded-2xl border border-rose-200 bg-rose-50/60 p-4">
              <p class="mb-1 text-xs font-black text-slate-600">ลิงก์วิดีโอ YouTube</p>
              <a
                :href="previewItem.youtubeUrl"
                target="_blank"
                rel="noopener noreferrer"
                class="flex items-center gap-1.5 break-all font-black text-teal-800 hover:underline"
              >
                <i class="mdi mdi-youtube text-xl text-rose-600" />
                <span>{{ previewItem.youtubeUrl }}</span>
              </a>
            </div>

            <div class="grid grid-cols-1 gap-2 border-t border-slate-300 pt-3 text-xs font-bold text-slate-600 sm:grid-cols-2">
              <div>สร้างเมื่อ: <span class="font-black text-slate-800">{{ formatDate(previewItem.createdAt) }}</span></div>
              <div>อัปเดตล่าสุด: <span class="font-black text-slate-800">{{ formatDate(previewItem.updatedAt) }}</span></div>
            </div>
          </div>

          <!-- MODAL ACTIONS -->
          <div
            class="sticky bottom-0 flex flex-wrap items-center justify-between gap-3 border-t border-slate-300 bg-white/95 px-6 py-4 backdrop-blur"
          >
            <div>
              <button
                v-if="previewItem.status === 'Archived'"
                type="button"
                class="inline-flex cursor-pointer items-center gap-1.5 rounded-xl bg-emerald-700 px-4 py-2.5 text-sm font-bold text-white shadow-xs transition hover:bg-emerald-800 active:scale-95"
                @click="restoreFromModal(previewItem)"
              >
                <i class="mdi mdi-shield-check-outline text-lg" />
                <span>ปลดระงับคอนเทนต์</span>
              </button>
              <button
                v-else
                type="button"
                class="inline-flex cursor-pointer items-center gap-1.5 rounded-xl border border-rose-300 bg-rose-50 px-4 py-2.5 text-sm font-bold text-rose-800 transition hover:bg-rose-600 hover:text-white"
                @click="archiveFromModal(previewItem)"
              >
                <i class="mdi mdi-shield-alert-outline text-lg" />
                <span>ระงับคอนเทนต์นี้</span>
              </button>
            </div>

            <div class="flex items-center gap-2">
              <button
                v-if="previewItem.status === 'Pending'"
                type="button"
                class="inline-flex cursor-pointer items-center gap-1.5 rounded-xl bg-emerald-700 px-5 py-2.5 text-sm font-black text-white shadow-md transition hover:bg-emerald-800 active:scale-95"
                @click="publishFromModal(previewItem)"
              >
                <i class="mdi mdi-check-circle-outline text-lg" />
                <span>อนุมัติและเผยแพร่</span>
              </button>

              <button
                type="button"
                class="cursor-pointer rounded-xl border border-slate-300 bg-white px-4 py-2.5 text-sm font-bold text-slate-700 transition hover:bg-slate-100"
                @click="previewItem = null"
              >
                ปิดหน้าต่าง
              </button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>
  </main>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import {
  archiveContent,
  getContentCategories,
  getContents,
  restoreContent,
  type Content,
  type ContentCategory,
} from '../api/adminContentApi'
import AppDataTable, {
  type DataTableColumn,
  type DataTablePagination,
} from '@/components/common/AppDataTable.vue'
import { formatDate } from '@/shared/utils/formatDate'
import { youtubeThumbnail } from '@/shared/utils/youtube'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'

const contents = ref<Content[]>([])
const categories = ref<ContentCategory[]>([])
const loading = ref(true)
const activeTab = ref<string>('All')
const selectedCategory = ref<string>('')
const searchQuery = ref<string>('')
const previewItem = ref<Content | null>(null)

const pagination = ref<DataTablePagination>({
  page: 1,
  pageSize: 15,
  totalCount: 0,
  totalPages: 0,
})

const stats = reactive({
  all: 0,
  published: 0,
  pending: 0,
  draft: 0,
  archived: 0,
})

const swal = useSwal()

const statusTabs = [
  { value: 'All', label: 'ทั้งหมด', icon: 'mdi-layers-outline' },
  { value: 'Published', label: 'เผยแพร่แล้ว', icon: 'mdi-check-circle-outline' },
  { value: 'Pending', label: 'รอตรวจสอบ', icon: 'mdi-clock-outline' },
  { value: 'Draft', label: 'ฉบับร่าง', icon: 'mdi-file-edit-outline' },
  { value: 'Archived', label: 'ถูกระงับ', icon: 'mdi-shield-alert-outline' },
]

const statCards = [
  {
    value: 'All',
    label: 'ทั้งหมด',
    hint: 'เผยแพร่และรอตรวจ',
    icon: 'mdi-layers-outline',
    bar: 'bg-slate-600',
    text: 'text-slate-900',
    iconBox: 'bg-slate-100 text-slate-700',
    activeBorder: 'border-slate-700',
    ring: 'ring-slate-500/15',
  },
  {
    value: 'Published',
    label: 'เผยแพร่แล้ว',
    hint: 'แสดงบนหน้าเว็บไซต์',
    icon: 'mdi-check-circle-outline',
    bar: 'bg-emerald-600',
    text: 'text-emerald-700',
    iconBox: 'bg-emerald-100 text-emerald-700',
    activeBorder: 'border-emerald-600',
    ring: 'ring-emerald-500/15',
  },
  {
    value: 'Pending',
    label: 'รอตรวจสอบ',
    hint: 'รอแอดมินอนุมัติ',
    icon: 'mdi-clock-outline',
    bar: 'bg-amber-500',
    text: 'text-amber-700',
    iconBox: 'bg-amber-100 text-amber-700',
    activeBorder: 'border-amber-500',
    ring: 'ring-amber-500/15',
  },
  {
    value: 'Draft',
    label: 'ฉบับร่าง',
    hint: 'ผู้ใช้ยังไม่เผยแพร่',
    icon: 'mdi-file-edit-outline',
    bar: 'bg-slate-400',
    text: 'text-slate-700',
    iconBox: 'bg-slate-100 text-slate-600',
    activeBorder: 'border-slate-500',
    ring: 'ring-slate-400/15',
  },
  {
    value: 'Archived',
    label: 'ถูกระงับ',
    hint: 'ฝ่าฝืนกฎ / ระงับแสดงผล',
    icon: 'mdi-shield-alert-outline',
    bar: 'bg-rose-600',
    text: 'text-rose-700',
    iconBox: 'bg-rose-100 text-rose-700',
    activeBorder: 'border-rose-600',
    ring: 'ring-rose-500/15',
  },
]

function thumbOf(content: Content): string {
  return youtubeThumbnail(content.youtubeUrl)
}

const columns: DataTableColumn[] = [
  { key: 'title', label: 'บทความ / คอนเทนต์' },
  { key: 'shop', label: 'ผู้สร้าง / ร้านค้า' },
  { key: 'status', label: 'สถานะ' },
  { key: 'actions', label: 'จัดการ' },
]

function asContent(item: object): Content {
  return item as Content
}

function getTabCount(tabValue: string): number {
  switch (tabValue) {
    case 'Published':
      return stats.published
    case 'Pending':
      return stats.pending
    case 'Draft':
      return stats.draft
    case 'Archived':
      return stats.archived
    default:
      return stats.all
  }
}

function statusLabel(status: string): string {
  switch (status) {
    case 'Published':
      return 'เผยแพร่แล้ว'
    case 'Pending':
      return 'รอตรวจสอบ'
    case 'Draft':
      return 'ฉบับร่าง'
    case 'Archived':
      return 'ถูกระงับ'
    default:
      return status
  }
}

function statusBadgeClass(status: string): string {
  switch (status) {
    case 'Published':
      return 'bg-emerald-100 text-emerald-800 border border-emerald-300'
    case 'Pending':
      return 'bg-amber-100 text-amber-800 border border-amber-300'
    case 'Draft':
      return 'bg-slate-100 text-slate-700 border border-slate-300'
    case 'Archived':
      return 'bg-rose-100 text-rose-800 border border-rose-300'
    default:
      return 'bg-slate-100 text-slate-700 border border-slate-300'
  }
}

function statusIcon(status: string): string {
  switch (status) {
    case 'Published':
      return 'mdi-check-circle'
    case 'Pending':
      return 'mdi-clock-outline'
    case 'Draft':
      return 'mdi-file-edit'
    case 'Archived':
      return 'mdi-shield-alert-outline'
    default:
      return 'mdi-help-circle'
  }
}

async function loadStats() {
  try {
    const [allRes, pubRes, pendRes, draftRes, archRes] = await Promise.allSettled([
      getContents({ page: 1, pageSize: 1 }),
      getContents({ status: 'Published', page: 1, pageSize: 1 }),
      getContents({ status: 'Pending', page: 1, pageSize: 1 }),
      getContents({ status: 'Draft', page: 1, pageSize: 1 }),
      getContents({ status: 'Archived', page: 1, pageSize: 1 }),
    ])

    if (pubRes.status === 'fulfilled') stats.published = pubRes.value.totalCount
    if (pendRes.status === 'fulfilled') stats.pending = pendRes.value.totalCount
    if (draftRes.status === 'fulfilled') stats.draft = draftRes.value.totalCount
    if (archRes.status === 'fulfilled') stats.archived = archRes.value.totalCount
    // ทั้งหมด ไม่รวม ฉบับร่าง และ ถูกระงับ
    stats.all = stats.published + stats.pending
  } catch {
    // Non-blocking stats error
  }
}

async function loadContents() {
  loading.value = true
  try {
    const result = await getContents({
      status: activeTab.value === 'All' ? null : activeTab.value,
      search: searchQuery.value.trim() || null,
      categoryId: selectedCategory.value || null,
      page: pagination.value.page,
      pageSize: pagination.value.pageSize,
    })
    // ทั้งหมด ไม่ต้องโชว์ สถานะถูกระงับ และ ฉบับร่าง
    const items = activeTab.value === 'All'
      ? result.items.filter((item) => item.status !== 'Archived' && item.status !== 'Draft')
      : result.items

    contents.value = items
    pagination.value = {
      page: result.page,
      pageSize: result.pageSize,
      totalCount: activeTab.value === 'All' ? stats.all || items.length : result.totalCount,
      totalPages: result.totalPages,
    }
  } catch (error) {
    await swal.error('โหลดข้อมูลคอนเทนต์ไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    loading.value = false
  }
}

async function loadCategories() {
  try {
    categories.value = await getContentCategories()
  } catch {
    // Non-blocking category error
  }
}

function selectTab(tab: string) {
  activeTab.value = tab
  pagination.value.page = 1
  loadContents()
}

function onFilterChange() {
  pagination.value.page = 1
  loadContents()
}

function clearSearch() {
  searchQuery.value = ''
  pagination.value.page = 1
  loadContents()
}

function changePage(page: number) {
  pagination.value.page = page
  loadContents()
}

function refreshData() {
  loadContents()
  loadStats()
}

function openPreview(content: Content) {
  previewItem.value = content
}

async function archive(content: Content) {
  const result = await swal.confirm(
    `ยืนยันการระงับคอนเทนต์ "${content.title}"?`,
    'คอนเทนต์นี้จะไม่แสดงบนหน้าเว็บไซต์อีกต่อไป เนื่องจากกระทำผิดกฎหรือนโยบายของระบบ'
  )
  if (!result.isConfirmed) return
  try {
    await archiveContent(content.contentId)
    await swal.success('ระงับคอนเทนต์เรียบร้อยแล้ว')
    await loadContents()
    await loadStats()
  } catch (error) {
    await swal.error('ดำเนินการไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  }
}

async function restore(content: Content) {
  const result = await swal.confirm(
    `ยืนยันการปลดระงับคอนเทนต์ "${content.title}"?`,
    'นำคอนเทนต์กลับมาแสดงผลบนเว็บไซต์ตามปกติ'
  )
  if (!result.isConfirmed) return
  try {
    await restoreContent(content.contentId, content)
    await swal.success('ปลดการระงับและนำคอนเทนต์กลับมาเผยแพร่เรียบร้อยแล้ว')
    await loadContents()
    await loadStats()
  } catch (error) {
    await swal.error('ดำเนินการไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  }
}

async function publish(content: Content) {
  const result = await swal.confirm(
    `ยืนยันการอนุมัติและเผยแพร่ "${content.title}"?`,
    'คอนเทนต์จะถูกนำขึ้นเผยแพร่สู่สาธารณะทันที'
  )
  if (!result.isConfirmed) return
  try {
    await restoreContent(content.contentId, content)
    await swal.success('อนุมัติและเผยแพร่คอนเทนต์เรียบร้อยแล้ว')
    await loadContents()
    await loadStats()
  } catch (error) {
    await swal.error('ดำเนินการไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  }
}

async function archiveFromModal(content: Content) {
  previewItem.value = null
  await archive(content)
}

async function restoreFromModal(content: Content) {
  previewItem.value = null
  await restore(content)
}

async function publishFromModal(content: Content) {
  previewItem.value = null
  await publish(content)
}

onMounted(() => {
  loadContents()
  loadStats()
  loadCategories()
})
</script>
