<template>
  <div id="app">
    <nav class="navbar navbar-expand-lg navbar-light bg-white border-bottom">
      <div class="container">
        <a class="navbar-brand" href="/">
          <img width="100" alt="ImgBot" src="/images/imgbot.svg">
        </a>
        <button
          class="navbar-toggler"
          type="button"
          data-toggle="collapse"
          data-target="#navbarSupportedContent"
          aria-controls="navbarSupportedContent"
          aria-expanded="false"
          aria-label="Toggle navigation"
        >
          <span class="navbar-toggler-icon"></span>
        </button>

        <div class="collapse navbar-collapse" id="navbarSupportedContent">
          <ul class="navbar-nav mr-auto">
            <li class="nav-item">
              <a class="nav-link h5 mb-0 pb-0" href="/docs">Docs</a>
            </li>
          </ul>
          <button
            class="btn btn-outline-success"
            v-if="loaded && !isauthenticated"
            v-on:click="signin"
          >Sign in</button>
          <button
            class="btn btn-outline-secondary"
            v-if="loaded && isauthenticated"
            v-on:click="signout"
          >Sign out</button>
        </div>
      </div>
    </nav>
    <div class="container" style="min-height: 250px">
      <h2 class="mb-5">ImgBot installations</h2>
      <div v-if="isauthenticated && loaded && installations.length === 0">
        <h3>No installations found</h3>
        <a
          class="btn btn-light border border-secondary"
          href="https://github.com/marketplace/imgbot"
        >Install now</a>
      </div>
      <div v-if="isauthenticated">
        <button
          v-on:click="select('all')"
          v-if="installations.length > 1"
          v-bind:class="{ active: this.selectedFilter === 'all' }"
          class="btn btn-outline-secondary"
        >All</button>
        <button
          v-on:click="select(installation.id)"
          v-bind:class="{ active: selectedFilter === installation.id }"
          class="btn btn-outline-secondary ml-2"
          v-for="installation in installations"
          v-bind:key="installation.id"
        >{{ installation.login }}</button>
        <a
          v-if="installations.length > 0"
          target="_blank"
          class="btn btn-light border border-secondary ml-2"
          href="https://github.com/marketplace/imgbot"
        >+</a>
      </div>
      <hr>
      <div>
        <loader v-if="!loaded"></loader>
        <button
          class="btn btn-success"
          v-if="loaded && !isauthenticated"
          v-on:click="signin"
        >Sign in</button>
        <installation
          v-for="installation in filteredInstallations"
          v-bind:key="installation.id"
          v-bind:installation="installation"
        ></installation>
      </div>
    </div>
    <footer>
      <div class="container pt-5">
        <div class="row">
          <div class="col-lg">
            <ul>
              <li>
                <a href="https://github.com/marketplace/category/code-quality">Marketplace</a>
              </li>
              <li>
                <a href="https://github.com/works-with/category/code-quality">Works with</a>
              </li>
              <li>
                <a href="https://github.com/apps/imgbot">Application</a>
              </li>
            </ul>
          </div>
          <div class="col-lg">
            <ul>
              <li>
                <a href="https://github.com/dabutvin/ImgBot/issues">Open an issue</a>
              </li>
              <li>
                <a href="mailto:help@imgbot.net">Email help@imgbot.net</a>
              </li>
              <li>
                <a href="/docs">Read the Docs</a>
              </li>
            </ul>
          </div>
          <div class="col-lg">
            <ul>
              <li>
                <a href="/privacy">Privacy policy</a>
              </li>
              <li>
                <a href="/terms">Terms of Service</a>
              </li>
              <li>
                <a href="/incident-response">Incident Response policy</a>
              </li>
              <li>
                <a href="/vulnerability-management">Vulnerability Management policy</a>
              </li>
            </ul>
          </div>
        </div>
        <div class="row mt-5">
          <div class="col">
            <p>
              <img class="copyright" alt src="/images/icon.svg" width="30">
              <span class>ImgBot &copy; 2017-2018</span>
            </p>
          </div>
        </div>
      </div>
    </footer>
  </div>
</template>

<script>
import { settings } from "./settings";
import Installation from "./components/Installation";
import Loader from "./components/Loader";

export default {
  name: "app",
  components: {
    Installation,
    Loader
  },
  data() {
    return {
      installations: [],
      isauthenticated: false,
      selectedFilter: "all",
      loaded: false
    };
  },
  methods: {
    signin: function(event) {
      window.location = `${settings.authhost}/api/setup?from=app`;
    },
    signout: function(event) {
      window.location = `${settings.authhost}/api/signout`;
    },
    select: function(value) {
      this.selectedFilter = value;
    }
  },
  mounted() {
    var vm = this;
    axios
      .get(`${settings.authhost}/api/isauthenticated`, {
        withCredentials: true
      })
      .then(response => {
        vm.isauthenticated = response.data.result;
        if (vm.isauthenticated) {
          axios
            .get(`${settings.authhost}/api/installations`, {
              withCredentials: true
            })
            .then(response => {
              this.loaded = true;
              vm.installations = response.data.installations;
            });
        } else {
          this.loaded = true;
        }
      });
  },
  computed: {
    filteredInstallations: function() {
      return this.installations.filter(x => {
        if (this.selectedFilter === "all") return true;
        return this.selectedFilter === x.id;
      });
    }
  }
};
</script>
