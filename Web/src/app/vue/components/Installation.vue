<template>
  <div>
    <div>
      <div>{{ id }}</div>
      <a :href="html_url">Manage</a>
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
  props: ['id', 'html_url'],
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
      .get(`${settings.authhost}/api/repositories/${vm.id}`, {
        withCredentials: true
      })
      .then(response => {
        vm.repositories = response.data.repositories
      })
  }
}
</script>
