<template>
  <div class="dots">
    <span></span>
    <span></span>
    <span></span>
  </div>
</template>

<script>
export default {
  name: 'Loader'
}
</script>

<style>
.dots span {
  border: 5px solid #666;
  border-radius: 100px;
  display: inline-block;

  -webkit-animation: load 1s infinite;
  animation: load 1s infinite;
}

.dots span:nth-child(2) {
  -webkit-animation-delay: 0.2s;
  animation-delay: 0.2s;
}

.dots span:nth-child(3) {
  -webkit-animation-delay: 0.4s;
  animation-delay: 0.4s;
}

@-webkit-keyframes load {
  0% {
    opacity: 0;
  }
  100% {
    opacity: 1;
  }
}
@keyframes load {
  0% {
    opacity: 0;
  }
  100% {
    opacity: 1;
  }
}
</style>
