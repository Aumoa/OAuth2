<script lang="ts">
import { defineComponent } from 'vue'

export default defineComponent({
  name: 'Switcher',
  props: {
    index: {
      type: Number,
      required: true,
      validator: (value: number) => Number.isInteger(value) && value >= 0,
    },
  },
  setup(props, { slots }) {
    return () => {
      if (!Number.isInteger(props.index) || props.index < 0) {
        return null
      }

      const children = slots.default?.() ?? []

      return children[props.index] ?? null
    }
  },
});
</script>
