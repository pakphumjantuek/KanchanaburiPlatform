<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'

type ValidationRule = (value: unknown) => boolean | string

interface Props {
  label?: string
  placeholder?: string
  items?: unknown[]
  disabled?: boolean
  readonly?: boolean
  clearable?: boolean
  multiple?: boolean
  searchable?: boolean
  itemTitle?: string
  itemValue?: string
  rules?: ValidationRule[]
}

const props = withDefaults(defineProps<Props>(), {
  label: '',
  placeholder: 'เลือกข้อมูล',
  items: () => [],
  disabled: false,
  readonly: false,
  clearable: false,
  multiple: false,
  searchable: true,
  itemTitle: '',
  itemValue: '',
  rules: () => [],
})

const model = defineModel<unknown>()

const isOpen = ref(false)
const searchQuery = ref('')
const containerRef = ref<HTMLElement | null>(null)
const dropdownRef = ref<HTMLElement | null>(null)
const searchInputRef = ref<HTMLInputElement | null>(null)
const dropdownStyle = ref<Record<string, string>>({})

function valueOf(item: unknown) {
  if (!props.itemValue || typeof item !== 'object' || item === null) return item
  return (item as Record<string, unknown>)[props.itemValue]
}

function titleOf(item: unknown) {
  if (item === null || item === undefined) return ''
  if (!props.itemTitle || typeof item !== 'object') return String(item)
  return String((item as Record<string, unknown>)[props.itemTitle] ?? '')
}

const selectedItem = computed(() => {
  if (model.value === null || model.value === undefined || model.value === '') return null
  return props.items.find((item) => valueOf(item) === model.value) ?? null
})

const selectedTitle = computed(() => {
  if (!selectedItem.value) return ''
  return titleOf(selectedItem.value)
})

const filteredItems = computed(() => {
  if (!searchQuery.value.trim()) return props.items
  const query = searchQuery.value.toLowerCase().trim()
  return props.items.filter((item) => titleOf(item).toLowerCase().includes(query))
})

const showSearchInput = computed(() => {
  if (!props.searchable) return false
  return props.items.length >= 5 || searchQuery.value.length > 0
})

function updatePosition() {
  if (!containerRef.value) return
  const rect = containerRef.value.getBoundingClientRect()
  const spaceBelow = window.innerHeight - rect.bottom
  const spaceAbove = rect.top
  const approxDropdownHeight = 280

  // If there's more space above than below, and space below is tight, open upwards
  const openUpward = spaceBelow < approxDropdownHeight && spaceAbove > spaceBelow

  if (openUpward) {
    dropdownStyle.value = {
      position: 'fixed',
      bottom: `${window.innerHeight - rect.top + 6}px`,
      left: `${rect.left}px`,
      width: `${rect.width}px`,
      maxHeight: `${Math.max(160, Math.min(340, spaceAbove - 20))}px`,
      zIndex: '99999',
    }
  } else {
    dropdownStyle.value = {
      position: 'fixed',
      top: `${rect.bottom + 6}px`,
      left: `${rect.left}px`,
      width: `${rect.width}px`,
      maxHeight: `${Math.max(160, Math.min(340, spaceBelow - 20))}px`,
      zIndex: '99999',
    }
  }
}

function toggleDropdown() {
  if (props.disabled || props.readonly) return
  if (!isOpen.value) {
    updatePosition()
  }
  isOpen.value = !isOpen.value
}

function selectItem(item: unknown) {
  model.value = valueOf(item)
  isOpen.value = false
  searchQuery.value = ''
}

function handleClear(e: MouseEvent) {
  e.stopPropagation()
  model.value = null
  searchQuery.value = ''
}

function handleClickOutside(event: MouseEvent) {
  const target = event.target as Node
  if (
    containerRef.value &&
    !containerRef.value.contains(target) &&
    dropdownRef.value &&
    !dropdownRef.value.contains(target)
  ) {
    isOpen.value = false
  }
}

function onScrollOrResize() {
  if (isOpen.value) {
    updatePosition()
  }
}

watch(isOpen, (newVal) => {
  if (newVal) {
    updatePosition()
    void nextTick(() => {
      updatePosition()
      if (showSearchInput.value) {
        searchInputRef.value?.focus({ preventScroll: true })
      }
    })
    window.addEventListener('scroll', onScrollOrResize, true)
    window.addEventListener('resize', onScrollOrResize)
  } else {
    searchQuery.value = ''
    window.removeEventListener('scroll', onScrollOrResize, true)
    window.removeEventListener('resize', onScrollOrResize)
  }
})

onMounted(() => {
  document.addEventListener('click', handleClickOutside)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', handleClickOutside)
  window.removeEventListener('scroll', onScrollOrResize, true)
  window.removeEventListener('resize', onScrollOrResize)
})
</script>

<template>
  <div
    ref="containerRef"
    class="relative w-full space-y-1 text-slate-800"
  >
    <!-- LABEL -->
    <label v-if="label" class="block text-xs font-semibold text-slate-700">
      {{ label }}
    </label>

    <!-- TRIGGER BUTTON -->
    <div
      class="relative flex min-h-[44px] w-full items-center justify-between rounded-xl border border-slate-300 bg-white px-3.5 py-2.5 text-xs sm:text-sm font-medium transition cursor-pointer shadow-2xs hover:border-slate-400 focus-within:border-indigo-600 focus-within:ring-2 focus-within:ring-indigo-500/20"
      :class="{
        'opacity-50 cursor-not-allowed bg-slate-100 text-slate-400': disabled || readonly,
        'border-indigo-600 ring-2 ring-indigo-500/20 shadow-xs': isOpen,
      }"
      @click="toggleDropdown"
    >
      <!-- DISPLAY TITLE OR PLACEHOLDER -->
      <span v-if="selectedItem" class="truncate font-semibold text-slate-900">
        {{ selectedTitle }}
      </span>
      <span v-else class="truncate text-slate-400 font-normal">
        {{ placeholder || 'เลือกข้อมูล' }}
      </span>

      <!-- ICONS -->
      <div class="flex items-center gap-1.5 shrink-0 ml-2">
        <button
          v-if="clearable && selectedItem && !disabled && !readonly"
          type="button"
          class="flex h-5 w-5 items-center justify-center rounded-full text-slate-400 hover:bg-slate-100 hover:text-slate-600 transition cursor-pointer"
          @click="handleClear"
          title="ล้างค่า"
        >
          <i class="mdi mdi-close text-xs" />
        </button>

        <i
          class="mdi mdi-chevron-down text-base text-slate-400 transition-transform duration-200"
          :class="{ 'rotate-180 text-indigo-600': isOpen }"
        />
      </div>
    </div>

    <!-- DROPDOWN MENU TELEPORTED TO BODY -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-150 ease-out"
        enter-from-class="transform scale-95 opacity-0"
        enter-to-class="transform scale-100 opacity-100"
        leave-active-class="transition duration-100 ease-in"
        leave-from-class="transform scale-100 opacity-100"
        leave-to-class="transform scale-95 opacity-0"
      >
        <div
          v-if="isOpen"
          ref="dropdownRef"
          :style="dropdownStyle"
          class="flex flex-col rounded-2xl border border-slate-200 bg-white py-2 shadow-2xl backdrop-blur-xl text-slate-800 font-sans"
        >
          <!-- SEARCH INPUT FIELD -->
          <div v-if="showSearchInput" class="px-2.5 pb-2 mb-1 border-b border-slate-100 shrink-0">
            <div class="relative flex items-center">
              <i class="mdi mdi-magnify absolute left-3 text-base text-slate-400" />
              <input
                ref="searchInputRef"
                v-model="searchQuery"
                type="text"
                class="w-full rounded-xl border border-slate-200 bg-slate-50 py-1.5 pl-9 pr-8 text-xs font-medium text-slate-800 outline-none placeholder:text-slate-400 focus:border-indigo-500 focus:bg-white focus:ring-1 focus:ring-indigo-500"
                placeholder="พิมพ์เพื่อค้นหา..."
                @click.stop
              />
              <button
                v-if="searchQuery"
                type="button"
                class="absolute right-2.5 text-xs text-slate-400 hover:text-slate-600 cursor-pointer"
                @click="searchQuery = ''"
              >
                <i class="mdi mdi-close-circle" />
              </button>
            </div>
          </div>

          <!-- CLEAR / ALL OPTION (IF CLEARABLE AND NO SEARCH QUERY) -->
          <div
            v-if="clearable && !searchQuery"
            class="flex items-center justify-between px-3.5 py-2 text-xs font-medium text-slate-600 hover:bg-slate-50 hover:text-slate-900 cursor-pointer transition shrink-0"
            :class="{ 'bg-indigo-50 text-indigo-700 font-semibold': model === null }"
            @click="selectItem(null)"
          >
            <span>{{ placeholder || 'ทั้งหมด' }}</span>
            <i v-if="model === null" class="mdi mdi-check text-sm text-indigo-600" />
          </div>

          <!-- OPTIONS LIST -->
          <div class="flex-1 overflow-y-auto space-y-0.5 px-1 scrollbar-thin">
            <div
              v-for="(item, index) in filteredItems"
              :key="index"
              class="flex items-center justify-between rounded-xl px-3 py-2 text-xs sm:text-sm font-medium transition cursor-pointer"
              :class="
                valueOf(item) === model
                  ? 'bg-indigo-50 text-indigo-700 font-semibold shadow-2xs'
                  : 'text-slate-700 hover:bg-slate-50 hover:text-slate-900'
              "
              @click="selectItem(item)"
            >
              <span class="truncate">{{ titleOf(item) }}</span>
              <i
                v-if="valueOf(item) === model"
                class="mdi mdi-check text-sm font-bold text-indigo-600 ml-2 shrink-0"
              />
            </div>

            <!-- NO RESULTS -->
            <div
              v-if="filteredItems.length === 0"
              class="px-3 py-4 text-center text-xs font-medium text-slate-400"
            >
              ไม่พบข้อมูลที่ค้นหา
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>
