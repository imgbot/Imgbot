<template>
  <div class="card my-4">
    <h5 class="card-title">{{ repository.id }}</h5>
    <h6 class="card-subtitle mb-2 text-muted">{{ repository.html_url }}</h6>
    <div class="card-text">{{ lastchecked }}</div>
  </div>
</template>

<script>
import moment from 'moment'

export default {
  name: 'Repository',
  props: ['repository'],
  computed: {
    lastchecked: function() {
      if (this.repository.lastchecked) {
        const ms = new Date().getTime() - new Date(this.repository.lastchecked).getTime()
        //console.log(new Date().getTime())
        const ago = moment.duration(ms, 'milliseconds').humanize()
        return `Last checked: ${ago} ago`
      } else {
        return 'Last checked: Never'
      }
    }
  }
}
</script>
