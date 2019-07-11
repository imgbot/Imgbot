<template>
  <div class="card my-4">
    <div class="card-body">
      <div class="row">
        <div class="col-lg-6">
          <h5 class="card-title">
            <octicon v-if="repository.fork" name="repo-forked"></octicon>
            <octicon v-if="!repository.fork" name="repo"></octicon>
            <a target="_blank" :href="current.html_url">{{ current.name }}</a>
          </h5>
          <div class="card-text">{{ lastchecked }}</div>
          <button v-on:click="check(current.id)" :disabled="checking" class="btn btn-secondary mt-4">
            <span v-if="!checking">Request new optimization</span>
            <span v-if="checking">Requesting ...</span>
          </button>
        </div>
        <div class="col-lg-6">
          <line-chart v-if="pulls.length" :data="chartData"></line-chart>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { settings } from '../settings'
import moment from 'moment'
import Octicon from 'vue-octicon/components/Octicon.vue'
import 'vue-octicon/icons/repo'
import 'vue-octicon/icons/repo-forked'

export default {
  name: 'Repository',
  props: ['repository', 'installationid', 'pulls'],
  components: {
    Octicon
  },
  data() {
    return {
      checking: false,
      current: this.repository
    }
  },
  computed: {
    lastchecked: function() {
      if (this.current.lastchecked) {
        const ms =
          new Date().getTime() - new Date(this.current.lastchecked).getTime()
        const ago = moment.duration(ms, 'milliseconds').humanize()
        return `The last optimization request was sent ${ago} ago`
      } else {
        return 'No optimization started recently'
      }
    },
    chartData: function() {
      return this.pulls.reduce((res, cur) => {
        res[cur.Timestamp] = cur.SpaceReduced
        return res
      }, {})
      return {'2017-05-13': 2, '2017-05-14': 5}
    }
  },
  methods: {
    check: function(repositoryid) {
      var vm = this

      vm.checking = true
      axios
        .get(
          `${settings.authhost}/api/repositories/check/${
            vm.installationid
          }/${repositoryid}`,
          {
            withCredentials: true
          }
        )
        .then(() => {
          var interval = setInterval(() => {
            axios
              .get(
                `${settings.authhost}/api/repositories/${
                  vm.installationid
                }/repository/${repositoryid}`,
                {
                  withCredentials: true
                }
              )
              .then(response => {
                if (
                  vm.current.lastchecked !==
                  response.data.repository.lastchecked
                ) {
                  clearInterval(interval)
                  vm.current = response.data.repository
                  vm.checking = false
                }
              })
          }, 30000)
        })
    }
  }
}
</script>
