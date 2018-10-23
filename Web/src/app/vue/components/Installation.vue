<template>
  <div>
    <div>
      <h3 class="text-left">
        <img class="rounded-circle" width="50" :src="installation.avatar_url" alt="">
        {{ installation.login }}
        </h3>
      <a target="_blank" :href="installation.html_url">Manage repos</a>
    </div>
    <div>
      <repository
        v-for="repository in repositories"
        v-bind:key="repository.id"
        v-bind:id="repository.id"
        v-bind:html_url="repository.html_url"
      ></repository>
    </div>
  </div>
</template>

<script>
import { settings } from '../settings'
import Repository from './Repository'

export default {
  name: 'Installation',
  props: ['installation'],
  components: {
    Repository
  },
  data() {
    return {
      repositories: []
    }
  },
  mounted() {
    var vm = this
    axios
      .get(`${settings.authhost}/api/repositories/${vm.installation.id}`, {
        withCredentials: true
      })
      .then(response => {
        vm.repositories = response.data.repositories
      })
  }
}
</script>
