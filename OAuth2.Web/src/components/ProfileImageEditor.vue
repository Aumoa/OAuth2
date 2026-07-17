<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import Dialog from '../core/components/Dialog.vue';

const props = defineProps<{
  isOpen: boolean;
  isSaving: boolean;
  hasImage: boolean;
  error?: string | null;
}>();

const emit = defineEmits<{
  'update:isOpen': [value: boolean];
  save: [image: Blob];
  remove: [];
}>();

const EDITOR_SIZE = 360;
const CROP_SIZE = 300;
const MAX_ZOOM = 8;

const { t } = useI18n();
const canvasElement = ref<HTMLCanvasElement | null>(null);
const fileInput = ref<HTMLInputElement | null>(null);
const image = ref<ImageBitmap | null>(null);
const fileName = ref('');
const zoom = ref(1);
const panX = ref(0);
const panY = ref(0);
const decodeError = ref<string | null>(null);
const isDragging = ref(false);
let lastPointerX = 0;
let lastPointerY = 0;

const canSave = computed(() => image.value !== null && !props.isSaving);
const displayedError = computed(() => decodeError.value ?? props.error ?? null);

function minimumScale(): number {
  if (!image.value) {
    return 1;
  }

  return Math.max(
    CROP_SIZE / image.value.width,
    CROP_SIZE / image.value.height,
  );
}

function clampPan(): void {
  if (!image.value) {
    panX.value = 0;
    panY.value = 0;
    return;
  }

  const scale = minimumScale() * zoom.value;
  const maximumX = Math.max(0, (image.value.width * scale - CROP_SIZE) / 2);
  const maximumY = Math.max(0, (image.value.height * scale - CROP_SIZE) / 2);
  panX.value = Math.max(-maximumX, Math.min(maximumX, panX.value));
  panY.value = Math.max(-maximumY, Math.min(maximumY, panY.value));
}

function drawEditor(): void {
  const canvas = canvasElement.value;
  const bitmap = image.value;
  if (!canvas || !bitmap) {
    return;
  }

  clampPan();
  const context = canvas.getContext('2d');
  if (!context) {
    return;
  }

  const scale = minimumScale() * zoom.value;
  const width = bitmap.width * scale;
  const height = bitmap.height * scale;
  const x = EDITOR_SIZE / 2 - width / 2 + panX.value;
  const y = EDITOR_SIZE / 2 - height / 2 + panY.value;

  context.clearRect(0, 0, EDITOR_SIZE, EDITOR_SIZE);
  context.imageSmoothingEnabled = true;
  context.imageSmoothingQuality = 'high';
  context.drawImage(bitmap, x, y, width, height);

  context.save();
  context.fillStyle = 'rgba(7, 12, 11, 0.62)';
  context.beginPath();
  context.rect(0, 0, EDITOR_SIZE, EDITOR_SIZE);
  context.arc(
    EDITOR_SIZE / 2,
    EDITOR_SIZE / 2,
    CROP_SIZE / 2,
    0,
    Math.PI * 2,
  );
  context.fill('evenodd');
  context.strokeStyle = 'rgba(255, 255, 255, 0.9)';
  context.lineWidth = 2;
  context.beginPath();
  context.arc(
    EDITOR_SIZE / 2,
    EDITOR_SIZE / 2,
    CROP_SIZE / 2,
    0,
    Math.PI * 2,
  );
  context.stroke();
  context.restore();
}

async function selectImageAsync(event: Event): Promise<void> {
  const input = event.currentTarget as HTMLInputElement;
  const file = input.files?.[0];
  if (!file) {
    return;
  }

  decodeError.value = null;
  try {
    const bitmap = await createImageBitmap(file, { imageOrientation: 'from-image' });
    image.value?.close();
    image.value = bitmap;
    fileName.value = file.name;
    zoom.value = 1;
    panX.value = 0;
    panY.value = 0;
    await nextTick();
    drawEditor();
  } catch {
    decodeError.value = t('app.accountInformation.profileImage.decodeFailed');
  } finally {
    input.value = '';
  }
}

function openFilePicker(): void {
  if (!props.isSaving) {
    fileInput.value?.click();
  }
}

function updateZoom(value: string): void {
  zoom.value = Math.max(1, Math.min(MAX_ZOOM, Number(value)));
  drawEditor();
}

function zoomWithWheel(event: WheelEvent): void {
  if (!image.value || props.isSaving) {
    return;
  }

  const direction = event.deltaY < 0 ? 1 : -1;
  zoom.value = Math.max(1, Math.min(MAX_ZOOM, zoom.value + direction * 0.15));
  drawEditor();
}

function pointerPosition(event: PointerEvent): [number, number] {
  const bounds = canvasElement.value?.getBoundingClientRect();
  if (!bounds || bounds.width === 0 || bounds.height === 0) {
    return [0, 0];
  }

  return [
    (event.clientX - bounds.left) * (EDITOR_SIZE / bounds.width),
    (event.clientY - bounds.top) * (EDITOR_SIZE / bounds.height),
  ];
}

function startDragging(event: PointerEvent): void {
  if (!image.value || props.isSaving) {
    return;
  }

  [lastPointerX, lastPointerY] = pointerPosition(event);
  isDragging.value = true;
  canvasElement.value?.setPointerCapture(event.pointerId);
}

function dragImage(event: PointerEvent): void {
  if (!isDragging.value || !image.value) {
    return;
  }

  const [x, y] = pointerPosition(event);
  panX.value += x - lastPointerX;
  panY.value += y - lastPointerY;
  lastPointerX = x;
  lastPointerY = y;
  drawEditor();
}

function stopDragging(event: PointerEvent): void {
  if (canvasElement.value?.hasPointerCapture(event.pointerId)) {
    canvasElement.value.releasePointerCapture(event.pointerId);
  }

  isDragging.value = false;
}

function canvasToBlob(canvas: HTMLCanvasElement): Promise<Blob> {
  return new Promise((resolve, reject) => {
    canvas.toBlob(blob => {
      if (blob) {
        resolve(blob);
      } else {
        reject(new Error('The profile image could not be encoded.'));
      }
    }, 'image/webp', 0.9);
  });
}

async function saveAsync(): Promise<void> {
  const bitmap = image.value;
  if (!bitmap || props.isSaving) {
    return;
  }

  decodeError.value = null;
  try {
    const output = document.createElement('canvas');
    output.width = 256;
    output.height = 256;
    const context = output.getContext('2d');
    if (!context) {
      throw new Error('Canvas is unavailable.');
    }

    const outputScale = minimumScale() * zoom.value * (256 / CROP_SIZE);
    const width = bitmap.width * outputScale;
    const height = bitmap.height * outputScale;
    const x = 128 - width / 2 + panX.value * (256 / CROP_SIZE);
    const y = 128 - height / 2 + panY.value * (256 / CROP_SIZE);
    context.imageSmoothingEnabled = true;
    context.imageSmoothingQuality = 'high';
    context.drawImage(bitmap, x, y, width, height);
    emit('save', await canvasToBlob(output));
  } catch {
    decodeError.value = t('app.accountInformation.profileImage.encodeFailed');
  }
}

function requestClose(value: boolean): void {
  if (!props.isSaving) {
    emit('update:isOpen', value);
  }
}

function reset(): void {
  image.value?.close();
  image.value = null;
  fileName.value = '';
  zoom.value = 1;
  panX.value = 0;
  panY.value = 0;
  decodeError.value = null;
  isDragging.value = false;
}

watch(() => props.isOpen, async isOpen => {
  if (!isOpen) {
    reset();
    return;
  }

  await nextTick();
  drawEditor();
});

onBeforeUnmount(reset);
</script>

<template>
  <Dialog
    :is-open="isOpen"
    :title="t('app.accountInformation.profileImage.title')"
    size="large"
    :close-on-backdrop="!isSaving"
    :close-on-escape="!isSaving"
    :show-close-button="!isSaving"
    @update:is-open="requestClose"
  >
    <div class="profile-image-editor">
      <p class="editor-description">
        {{ t('app.accountInformation.profileImage.description') }}
      </p>

      <input
        ref="fileInput"
        class="visually-hidden"
        type="file"
        accept="image/*"
        :disabled="isSaving"
        @change="selectImageAsync"
      />

      <div v-if="image" class="editor-stage" :class="{ dragging: isDragging }">
        <canvas
          ref="canvasElement"
          :width="EDITOR_SIZE"
          :height="EDITOR_SIZE"
          :aria-label="t('app.accountInformation.profileImage.cropAreaLabel')"
          @pointerdown="startDragging"
          @pointermove="dragImage"
          @pointerup="stopDragging"
          @pointercancel="stopDragging"
          @wheel.prevent="zoomWithWheel"
        />
      </div>

      <button
        v-else
        type="button"
        class="image-picker"
        :disabled="isSaving"
        @click="openFilePicker"
      >
        <span class="material-symbols-outlined" aria-hidden="true">add_photo_alternate</span>
        <strong>{{ t('app.accountInformation.profileImage.selectAction') }}</strong>
        <span>{{ t('app.accountInformation.profileImage.selectHint') }}</span>
      </button>

      <div v-if="image" class="editor-controls">
        <button
          type="button"
          class="app-button select-another-button"
          :disabled="isSaving"
          @click="openFilePicker"
        >
          <span class="material-symbols-outlined" aria-hidden="true">image</span>
          <span>{{ t('app.accountInformation.profileImage.selectAnotherAction') }}</span>
        </button>
        <span v-if="fileName" class="selected-file-name" :title="fileName">{{ fileName }}</span>

        <label class="zoom-control">
          <span class="material-symbols-outlined" aria-hidden="true">zoom_out</span>
          <input
            :value="zoom"
            type="range"
            min="1"
            :max="MAX_ZOOM"
            step="0.01"
            :disabled="isSaving"
            :aria-label="t('app.accountInformation.profileImage.zoomLabel')"
            @input="updateZoom(($event.currentTarget as HTMLInputElement).value)"
          />
          <span class="material-symbols-outlined" aria-hidden="true">zoom_in</span>
        </label>
        <p class="drag-hint">{{ t('app.accountInformation.profileImage.dragHint') }}</p>
      </div>

      <p v-if="displayedError" class="editor-error" role="alert">{{ displayedError }}</p>
    </div>

    <template #footer>
      <button
        type="button"
        class="app-button editor-action"
        :disabled="isSaving"
        @click="requestClose(false)"
      >
        {{ t('app.accountInformation.profileImage.cancelAction') }}
      </button>
      <button
        v-if="hasImage"
        type="button"
        class="app-button editor-action remove"
        :disabled="isSaving"
        @click="emit('remove')"
      >
        {{ t('app.accountInformation.profileImage.removeAction') }}
      </button>
      <button
        type="button"
        class="app-button editor-action primary"
        :disabled="!canSave"
        @click="saveAsync"
      >
        <span v-if="isSaving" class="material-symbols-outlined spinner" aria-hidden="true">
          progress_activity
        </span>
        <span>{{ t('app.accountInformation.profileImage.saveAction') }}</span>
      </button>
    </template>
  </Dialog>
</template>

<style scoped lang="css">
.profile-image-editor {
  display: grid;
  gap: 18px;
}

.editor-description,
.drag-hint {
  margin: 0;
  color: var(--text-muted);
  font-size: 13px;
  line-height: 1.5;
}

.visually-hidden {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}

.editor-stage {
  width: min(100%, 360px);
  margin-inline: auto;
  overflow: hidden;
  border: 1px solid var(--border);
  border-radius: 14px;
  background:
    linear-gradient(45deg, var(--surface-muted) 25%, transparent 25%),
    linear-gradient(-45deg, var(--surface-muted) 25%, transparent 25%),
    linear-gradient(45deg, transparent 75%, var(--surface-muted) 75%),
    linear-gradient(-45deg, transparent 75%, var(--surface-muted) 75%),
    var(--surface);
  background-position: 0 0, 0 8px, 8px -8px, -8px 0;
  background-size: 16px 16px;
  cursor: grab;
  touch-action: none;
}

.editor-stage.dragging {
  cursor: grabbing;
}

.editor-stage canvas {
  display: block;
  width: 100%;
  height: auto;
}

.image-picker {
  display: grid;
  min-height: 250px;
  padding: 32px;
  place-items: center;
  align-content: center;
  gap: 8px;
  color: var(--text-muted);
  border: 1px dashed var(--accent-border);
  border-radius: 14px;
  background: color-mix(in srgb, var(--accent-bg) 45%, transparent);
  cursor: pointer;
  transition: border-color 140ms ease, background-color 140ms ease, color 140ms ease;
}

.image-picker:hover:not(:disabled) {
  color: var(--text-h);
  border-color: var(--accent);
  background: var(--accent-bg);
}

.image-picker:focus-visible {
  outline: 3px solid var(--accent-border);
  outline-offset: 3px;
}

.image-picker .material-symbols-outlined {
  color: var(--accent);
  font-size: 44px;
}

.image-picker strong {
  color: var(--text-h);
  font-size: 15px;
}

.image-picker span:last-child {
  font-size: 12px;
  font-weight: 500;
}

.editor-controls {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr);
  gap: 12px;
  align-items: center;
}

.select-another-button {
  display: inline-flex;
  gap: 7px;
  align-items: center;
  padding: 8px 12px;
  color: var(--text);
  border: 1px solid var(--border);
  border-radius: 9px;
  background: var(--surface);
  cursor: pointer;
}

.select-another-button .material-symbols-outlined {
  font-size: 18px;
}

.selected-file-name {
  min-width: 0;
  overflow: hidden;
  color: var(--text-muted);
  font-size: 12px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.zoom-control {
  display: grid;
  grid-column: 1 / -1;
  grid-template-columns: auto minmax(0, 1fr) auto;
  gap: 10px;
  align-items: center;
}

.zoom-control .material-symbols-outlined {
  color: var(--text-muted);
  font-size: 20px;
}

.zoom-control input {
  width: 100%;
  accent-color: var(--accent);
}

.drag-hint {
  grid-column: 1 / -1;
  text-align: center;
}

.editor-error {
  margin: 0;
  padding: 10px 12px;
  color: var(--danger);
  border: 1px solid color-mix(in srgb, var(--danger) 36%, var(--border));
  border-radius: 9px;
  background: color-mix(in srgb, var(--danger) 8%, transparent);
  font-size: 13px;
}

.editor-action {
  min-height: 38px;
  padding: 8px 14px;
  color: var(--text);
  border: 1px solid var(--border);
  border-radius: 9px;
  background: var(--surface);
  cursor: pointer;
}

.editor-action.remove {
  margin-right: auto;
  color: var(--danger);
  border-color: color-mix(in srgb, var(--danger) 35%, var(--border));
}

.editor-action.primary {
  display: inline-flex;
  gap: 7px;
  align-items: center;
  color: var(--on-accent);
  border-color: var(--accent);
  background: var(--accent);
}

.editor-action:disabled,
.image-picker:disabled,
.select-another-button:disabled {
  cursor: not-allowed;
  opacity: 0.58;
}

.spinner {
  font-size: 18px;
  animation: profile-image-spin 800ms linear infinite;
}

@keyframes profile-image-spin {
  to {
    transform: rotate(360deg);
  }
}

@media (max-width: 540px) {
  .editor-controls {
    grid-template-columns: 1fr;
  }

  .selected-file-name,
  .select-another-button {
    width: 100%;
    box-sizing: border-box;
  }

  .zoom-control,
  .drag-hint {
    grid-column: 1;
  }
}

@media (prefers-reduced-motion: reduce) {
  .spinner {
    animation-duration: 1ms;
  }
}
</style>
