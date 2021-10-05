const webpackConfig = require('./webpack.config')

module.exports = function(grunt) {
  grunt.loadNpmTasks('grunt-contrib-clean')
  grunt.loadNpmTasks('grunt-markdown')
  grunt.loadNpmTasks('grunt-contrib-less')
  grunt.loadNpmTasks('grunt-contrib-cssmin')
  grunt.loadNpmTasks('grunt-contrib-copy')
  grunt.loadNpmTasks('grunt-devserver')
  grunt.loadNpmTasks('grunt-contrib-watch')
  grunt.loadNpmTasks('grunt-webpack')
  grunt.loadNpmTasks('grunt-postcss')
  grunt.loadTasks('./tasks')

  grunt.initConfig({
    clean: ['./dist'],
    markdown: {
      all: {
        files: [
          {
            cwd: 'src',
            expand: true,
            src: '**/*.md',
            dest: 'dist/.',
            ext: '.html'
          },
          {
            cwd: 'src',
            expand: true,
            src: '**/*.html',
            dest: 'dist/.',
            ext: '.html'
          }
        ],
        options: {
          template: 'src/layout.jst',
          contextBinder: true,
          headerIds: false,
          gfm: true
        }
      }
    },
    less: {
      all: {
        files: {
          'dist/css/site.css': 'src/css/site.less'
        }
      }
    },
    postcss: {
      options: {
        processors: [
          require('autoprefixer')()
        ]
      },
      dist: {
        src: 'dist/css/*.css'
      }
    },
    cssmin: {
      target: {
        files: {
          'dist/css/site.min.css': 'dist/css/site.css'
        }
      }
    },
    copy: {
      all: {
        files: [
          {
            expand: true,
            cwd: 'node_modules',
            src: [
              'bootstrap/dist/**',
              'jquery/dist/**',
              'axios/dist/**',
              'vue/dist/**'
            ],
            dest: 'dist/lib/'
          },
          {
            expand: true,
            cwd: 'src',
            src: ['favicon.ico', 'images/**'],
            dest: 'dist'
          }
        ]
      }
    },
    webpack: {
      prod: Object.assign({ mode: 'production' }, webpackConfig),
      dev: Object.assign({ watch: true, mode: 'development' }, webpackConfig)
    },
    watch: {
      site: {
        files: ['src/**/*.*'],
        tasks: ['gen'],
        options: {
          spawn: false
        }
      },
      webpack: {
        files: ['src/app/**/*.*'],
        tasks: ['webpack:dev'],
        options: {
          spawn: false
        }
      }
    },
    devserver: {
      dist: {
        options: {
          base: 'dist'
        }
      }
    }
  })

  grunt.registerTask('gen', [
    'clean',
    'compile-docs',
    'markdown:all',
    'less:all',
    'postcss',
    'cssmin',
    'copy'
  ])
}
