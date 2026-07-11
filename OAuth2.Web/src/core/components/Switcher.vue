<script lang="ts">
import { cloneVNode, defineComponent } from 'vue'

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
      const selectedChild = children[props.index]

      if (!selectedChild) {
        return null
      }

      // A slot may contain unkeyed children of the same element type. Give the
      // selected child an index-based identity so Vue replaces its DOM node and
      // event handlers when the active item changes.
      return cloneVNode(selectedChild, { key: props.index })
    }
  },
});
</script>
