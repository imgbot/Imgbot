<template>
  <div>
    <div class="card my-4">
      <div class="card-header">
        <button
          type="button"
          class="btn btn-sm float-right"
          data-toggle="modal"
          v-on:click="loadSettings"
          :data-target="`#settings_${current.id}`"
        >
          <octicon name="gear"></octicon>
        </button>
        <h5 class="card-title mt-1">
          <octicon v-if="repository.fork" name="repo-forked"></octicon>
          <octicon v-if="!repository.fork" name="repo"></octicon>
          <a target="_blank" :href="current.html_url">{{ current.name }}</a>
        </h5>
      </div>
      <div class="card-body">
        <div class="card-text">{{ lastchecked }}</div>
        <button v-on:click="check" :disabled="checking" class="btn btn-secondary mt-4">
          <span v-if="!checking">Request new optimization</span>
          <span v-if="checking">Requesting ...</span>
        </button>
      </div>
    </div>
    <div
      class="modal fade"
      :id="`settings_${current.id}`"
      tabindex="-1"
      role="dialog"
      aria-hidden="true"
    >
      <div class="modal-dialog" role="document">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Settings for "{{ current.name }}"</h5>
            <button type="button" class="close" data-dismiss="modal" aria-label="Close">
              <span aria-hidden="true">&times;</span>
            </button>
          </div>
          <div class="modal-body">
            <form>
              <div class="form-group row">
                <label
                  class="col-sm-5 col-form-label"
                  :for="`default-branch-override-${current.id}`"
                >Default branch override:</label>
                <div class="col-sm-7">
                  <input
                    type="text"
                    class="form-control"
                    :id="`default-branch-override-${current.id}`"
                    placeholder="(optional)"
                    v-model="settings.DefaultBranchOverride"
                  />
                  <small>If you want Imgbot to look after a different branch instead of the default for the repo.</small>
                </div>
              </div>
            </form>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancel</button>
            <button v-on:click="saveSettings" type="button" class="btn btn-success">Save changes</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { settings } from "../settings";
import moment from "moment";
import Octicon from "vue-octicon/components/Octicon.vue";
import "vue-octicon/icons/repo";
import "vue-octicon/icons/repo-forked";
import "vue-octicon/icons/gear";

export default {
  name: "Repository",
  props: ["repository", "installationid"],
  components: {
    Octicon
  },
  data() {
    return {
      checking: false,
      current: this.repository,
      settings: {}
    };
  },
  computed: {
    lastchecked: function() {
      if (this.current.lastchecked) {
        const ms =
          new Date().getTime() - new Date(this.current.lastchecked).getTime();
        const ago = moment.duration(ms, "milliseconds").humanize();
        return `The last optimization request was sent ${ago} ago`;
      } else {
        return "No optimization started recently";
      }
    }
  },
  methods: {
    check: function() {
      var vm = this;

      vm.checking = true;

      axios
        .get(
          `${settings.authhost}/api/repositories/check/${vm.installationid}/${vm.current.id}`,
          {
            withCredentials: true
          }
        )
        .then((checkResponse) => {
          if (checkResponse.data.status === "branchexists") {
            alert("The Imgbot branch already exists in this repo. If you want a new round of optimization please delete this branch and try again.");
            vm.checking = false;
          } else {
            var interval = setInterval(() => {
              axios
                .get(
                  `${settings.authhost}/api/repositories/${vm.installationid}/repository/${vm.current.id}`,
                  {
                    withCredentials: true
                  }
                )
                .then(response => {
                  if (
                    vm.current.lastchecked !==
                    response.data.repository.lastchecked
                  ) {
                    clearInterval(interval);
                    vm.current = response.data.repository;
                    vm.checking = false;
                  }
                });
            }, 30000);
          }
        });
    },
    loadSettings: function() {
      var vm = this;

      axios
        .get(
          `${settings.authhost}/api/repositories/settings/${vm.installationid}/${vm.current.id}`,
          {
            withCredentials: true
          }
        )
        .then(response => {
          vm.settings = response.data || {};
        });
    },
    saveSettings: function() {
      var vm = this;

      axios
        .post(
          `${settings.authhost}/api/repositories/settings/${vm.installationid}/${vm.current.id}`,
          vm.settings,
          {
            withCredentials: true
          }
        )
        .then(() => {
          $(`#settings_${vm.current.id}`).modal("hide");
          vm.check();
        });
    }
  }
};
</script>
