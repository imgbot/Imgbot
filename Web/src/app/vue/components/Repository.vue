<template>
  <div>
    <div>{{ id }}</div>
    <div>{{ html_url }}</div>
  </div>
</template>

<script>
export default {
  name: 'Repository',
  props: ['id', 'html_url']
}
</script>
