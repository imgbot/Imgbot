<template>
  <div class="card my-4">
    <h5 class="card-title">{{ id }}</h5>
    <h6 class="card-subtitle mb-2 text-muted">{{ html_url }}</h6>
  </div>
</template>

<script>
export default {
  name: 'Repository',
  props: ['id', 'html_url']
}
</script>
