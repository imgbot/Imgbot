<template>
    <div id="app">
        <div class="container">
            <nav role="navigation">
                <InstallationPicker v-bind:installations="installations"></InstallationPicker>
                <button v-if="!isauthenticated" v-on:click="signin">Sign in</button>
                <button v-if="isauthenticated" v-on:click="signout">Sign out</button>
            </nav>
            <div>
                <installation
                    v-for="installation in installations"
                    v-bind:key="installation.id"
                    v-bind:id="installation.id"
                    v-bind:html_url="installation.html_url"
                ></installation>
            </div>
        </div>
    </div>
</template>

<script>
import { settings } from './settings'
import InstallationPicker from './components/InstallationPicker'
import Installation from './components/Installation'

export default {
  name: 'app',
  components: {
    InstallationPicker,
    Installation
  },
  data() {
    return {
      installations: [],
      isauthenticated: false
    }
  },
  methods: {
    signin: function(event) {
      window.location = `${settings.authhost}/api/setup`
    },
    signout: function(event) {
      window.location = `${settings.authhost}/api/signout`
    }
  },
  mounted() {
    var vm = this
    axios
      .get(`${settings.authhost}/api/isauthenticated`, {
        withCredentials: true
      })
      .then(response => {
        vm.isauthenticated = response.data.result
        if (vm.isauthenticated) {
          axios
            .get(`${settings.authhost}/api/installations`, {
              withCredentials: true
            })
            .then(response => {
              vm.installations = response.data.installations
            })
        }
      })
  }
}
</script>
