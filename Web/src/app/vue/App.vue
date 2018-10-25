<template>
    <div id="app">
        <nav class="navbar navbar-expand-lg navbar-light bg-light">
            <div class="container">
              <a class="navbar-brand" href="#"><img width="100" alt="ImgBot" src="/images/imgbot.svg" /></a>
              <button class="navbar-toggler" type="button" data-toggle="collapse" data-target="#navbarSupportedContent" aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation">
                <span class="navbar-toggler-icon"></span>
              </button>

              <div class="collapse navbar-collapse" id="navbarSupportedContent">
                <ul class="navbar-nav mr-auto">
                  <InstallationPicker v-bind:installations="installations"></InstallationPicker>
                  <li class="nav-item">
                    <a class="nav-link" href="/docs">Docs</a>
                  </li>
                </ul>
              <button class="btn btn-outline-success" v-if="!isauthenticated" v-on:click="signin">Sign in</button>
              <button class="btn btn-outline-secondary" v-if="isauthenticated" v-on:click="signout">Sign out</button>
              </div>
            </div>
        </nav>
        <div class="container" style="min-height: 250px">
            <h2>ImgBot installations</h2>
            <div>
                <installation
                    v-for="installation in installations"
                    v-bind:key="installation.id"
                    v-bind:installation="installation"
                ></installation>
            </div>
        </div>
        <footer>
          <div class="container">
            <ul>
              <li>ImgBot &copy; 2017-2018</li>
              <li><a href="mailto:help@imgbot.net">help@imgbot.net</a></li>
              <li><a href="/privacy">Privacy policy</a></li>
              <li><a href="/terms">Terms of Service</a></li>
              <li><a href="/incident-response">Incident Response policy</a></li>
              <li><a href="/vulnerability-management">Vulnerability Management policy</a></li>
              <li><a href="https://github.com/dabutvin/ImgBot/issues">Open an issue</a></li>
              <li><a href="/docs">Docs</a></li>
              <li><a href="https://github.com/works-with/category/code-quality">Works with GitHub</a></li>
              <li><a href="https://github.com/marketplace/category/code-quality">GitHub Marketplace</a></li>
            </ul>
          </div>
      </footer>
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
