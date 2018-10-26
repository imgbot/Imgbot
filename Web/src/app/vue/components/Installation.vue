<template>
  <div>
    <div>
      <h3 class="text-left d-inline-block" style="text-transform: none" :id="installation.login">
        <img class="rounded-circle" width="50" :src="installation.avatar_url" alt="">
        {{ installation.login }}
      </h3>
      <h5 class="d-inline-block mb-4 align-bottom ml-3"><span class="badge badge-info">{{ this.plan }}</span></h5>
      <div><a target="_blank" :href="installation.html_url">Manage repos</a></div>
    </div>
    <div>
      <repository
        v-for="repository in repositories"
        v-bind:key="repository.id"
        v-bind:repository="repository"
        v-bind:installationid="installation.id"
      ></repository>
    </div>
    <hr>
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
  computed: {
    plan: function() {
      switch (this.installation.planId) {
        case 781:
          return 'Early adopter plan'
        case 111:
          return 'Open source plan'
        case 999:
          return 'Private repos plan'
      }
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
