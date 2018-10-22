<template>
    <div id="app">
        <div class="container">
            <nav role="navigation">
                <button v-if="!isauthenticated" v-on:click="signin">Sign in</button>
                <button v-if="isauthenticated" v-on:click="signout">Sign out</button>
            </nav>
            <ul>
                <li v-for="installation in installations" v-bind:key="installation.id">
                    {{ installation.id }}
                    {{ installation.html_url }}
                </li>
            </ul>
        </div>
    </div>
</template>

<script>
import { settings } from './settings'

export default {
  name: 'app',
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
