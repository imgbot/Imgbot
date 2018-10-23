<template>
  <ul>
    <li 
      v-for="installation in installations"
      v-bind:key="installation.id">
        <a>{{ installation.id }}</a>
    </li>
  </ul>
</template>
<script>
export default {
  name: 'InstallationPicker',
  props: ['installations']
}
</script>
