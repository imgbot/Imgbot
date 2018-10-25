<template>
  <li class="nav-item dropdown">
    <a class="nav-link dropdown-toggle" href="#" id="navbarDropdown" role="button" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
      Installations
    </a>
    <div class="dropdown-menu" aria-labelledby="navbarDropdown">
    <a class="dropdown-item" :href="'#' + installation.login"
      v-for="installation in installations"
      v-bind:key="installation.id">
      <img class="rounded-circle" width="20" :src="installation.avatar_url" alt="">
      {{ installation.login }}
    </a>
    <div class="dropdown-divider"></div>
    <a class="dropdown-item" href="https://github.com/marketplace/imgbot">Add more</a>
  </div>
  </li>
</template>
<script>
export default {
  name: 'InstallationPicker',
  props: ['installations']
}
</script>
