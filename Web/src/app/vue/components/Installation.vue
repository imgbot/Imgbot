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
        <button v-on:click="togglePrivate()" type="button" v-bind:class="[onlyPrivate ? 'btn-success' : 'btn-primary']" class="btn">{{this.privateDisplay}}</button>
        <h5 class="d-inline-block" v-if="limit"><span class="btn btn-danger"> You have reached the limit of private repositories you can optimize</span></h5>
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
      <repository @updatedUsedRepos = "updateUsedRepos"
        v-for="repository in filteredRepositories"
        v-bind:key="repository.id"
        v-bind:repository="repository"
        v-bind:installationid="installation.id"
        v-bind:planId="installation.planId"
        v-bind:limit="limit"
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
      loaded: false,
      onlyPrivate: false
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
        case 6927:
          return 'Open source plan'
        case 1750:
          return 'Premium plan'
        case 2840:
          return 'Individual plan'
        case 2841:
          return 'Professional plan'
        case 6894:
          return 'Starter'
        case 6919:
        case 7386:
          return 'Team'
        case 6920:
        case 7387:
          return 'Agency'
        case 6921:
        case 7388:
          return 'Enterprise'
        case 6922:
        case 7389:
          return 'Gold'
        case 6923:
        case 7390:
          return 'Platinium'

      }
    },
    changePlan: function() {
      switch (this.installation.planId) {
        case 1749:
        case 6894:
        case 6919:
        case 6920:
        case 6921:
        case 6922:
        case 6927:
        case 7386:
        case 7387:
        case 7388:
        case 7389:
          return 'Upgrade plan'
        case 1750:
        case 2840:
        case 2841:
        case 6923:
        case 7390:
          return 'Downgrade plan'
      }
    },
    changePlanLink: function() {
      switch (this.installation.planId) {
        case 1749:
        case 6927:
          return `https://github.com/marketplace/imgbot/upgrade/13/${this.installation.accountid}`
        case 1750:
        case 2840:
        case 2841:
          return `https://github.com/marketplace/imgbot/upgrade/12/${this.installation.accountid}`
        case 6894:
          return `https://github.com/marketplace/imgbot/upgrade/13/${this.installation.accountid}`
        case 6919:
          return `https://github.com/marketplace/imgbot/upgrade/14/${this.installation.accountid}`
        case 6920:
          return `https://github.com/marketplace/imgbot/upgrade/15/${this.installation.accountid}`
        case 6921:
          return `https://github.com/marketplace/imgbot/upgrade/16/${this.installation.accountid}`
        case 6922:
          return `https://github.com/marketplace/imgbot/upgrade/17/${this.installation.accountid}`
        case 6923:
          return `https://github.com/marketplace/imgbot/upgrade/16/${this.installation.accountid}`
        case 7386:
          return `https://github.com/marketplace/imgbot/upgrade/14/${this.installation.accountid}`
        case 7387:
          return `https://github.com/marketplace/imgbot/upgrade/15/${this.installation.accountid}`
        case 7388:
          return `https://github.com/marketplace/imgbot/upgrade/16/${this.installation.accountid}`
        case 7389:
          return `https://github.com/marketplace/imgbot/upgrade/17/${this.installation.accountid}`
        case 7390:
          return `https://github.com/marketplace/imgbot/upgrade/16/${this.installation.accountid}`
      }
    },
    filteredRepositories: function() {
      return this.repositories.filter(x => {
        if ( this.onlyPrivate === true ) return x.IsPrivate;
        if (!this.repofilter.length) return true
        return x.name.toLowerCase().startsWith(this.repofilter.toLowerCase())
      })
    },
    limit () {
      return Object.prototype.hasOwnProperty.call(this.installation, 'allowedPrivate')
          && this.installation.allowedPrivate !== null
          && Object.prototype.hasOwnProperty.call(this.installation, 'usedPrivate')
          && this.installation.allowedPrivate <= this.installation.usedPrivate;
    },
    privateDisplay: function() {
      return this.onlyPrivate ? "Show all public and private repositories used" : "Show only private repos";
    }
  },
  methods : {
    togglePrivate () {
      this.onlyPrivate = ! this.onlyPrivate;
    },
    updateUsedRepos ( updatedValue ) {
      this.installation.usedPrivate = updatedValue;
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
