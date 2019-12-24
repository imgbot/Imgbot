<template>
  <div>
    <div>
      <h3 class="text-left d-inline-block" style="text-transform: none" :id="installation.login">
        <img class="rounded-circle" width="50" :src="installation.avatar_url" alt="">
        {{ installation.login }}
      </h3>
      <h5 class="d-inline-block mb-4 align-bottom ml-3"><span class="badge badge-info">{{ this.plan }}</span></h5>
      <div>
        <a class="btn btn-outline-secondary btn-sm" target="_blank" :href="installation.html_url">Manage repos</a>
        <a class="btn btn-outline-secondary btn-sm" v-if="changePlan" target="_blank" :href="changePlanLink">{{ this.changePlan }}</a>
      </div>
      <div class="mt-4" v-if="repositories.length > 2">
        <input placeholder="Search" class="w-25" type="text" v-model="repofilter">
        <button style="margin-left: -36px"><octicon name="search"></octicon></button>
      </div>
    </div>
    <div>
      <p class="mt-4" v-if="loaded && filteredRepositories.length < 1">No repos found</p>
      <loader v-if="!loaded"></loader>
      <repository
        v-for="repository in filteredRepositories"
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
import Loader from './Loader'
import Octicon from 'vue-octicon/components/Octicon.vue'
import 'vue-octicon/icons/search'

export default {
  name: 'Installation',
  props: ['installation'],
  components: {
    Repository,
    Octicon,
    Loader
  },
  data() {
    return {
      repositories: [],
      repofilter: '',
      loaded: false
    }
  },
  computed: {
    plan: function() {
      if (this.installation.student) {
        return 'Student'
      }
      switch (this.installation.planId) {
        case 781:
          return 'Early adopter plan'
        case 1749:
          return 'Open source plan'
        case 1750:
          return 'Premium plan'
        case 2840:
          return 'Individual plan'
        case 2841:
          return 'Professional plan'
      }
    },
    changePlan: function() {
      switch (this.installation.planId) {
        case 1749:
          return 'Upgrade plan'
        case 1750:
        case 2840:
        case 2841:
          return 'Downgrade plan'
      }
    },
    changePlanLink: function() {
      switch (this.installation.planId) {
        case 1749:
          return this.installation.accounttype === 'User' ?
            `https://github.com/marketplace/imgbot/upgrade/4/${this.installation.accountid}` :
            `https://github.com/marketplace/imgbot/upgrade/5/${this.installation.accountid}`
        case 1750:
        case 2840:
        case 2841:
          return `https://github.com/marketplace/imgbot/upgrade/2/${this.installation.accountid}`
      }
    },
    filteredRepositories: function() {
      return this.repositories.filter(x => {
        if (!this.repofilter.length) return true
        return x.name.toLowerCase().startsWith(this.repofilter.toLowerCase())
      })
    }
  },
  mounted() {
    var vm = this

    function fetchRepos(page) {
      axios
        .get(
          `${settings.authhost}/api/repositories/${vm.installation.id}/${page}`,
          {
            withCredentials: true
          }
        )
        .then(response => {
          vm.loaded = true
          vm.repositories = vm.repositories.concat(response.data.repositories)
          if (response.data.next) {
            fetchRepos(response.data.next)
          }
        })
    }

    fetchRepos(1)
  }
}
</script>
